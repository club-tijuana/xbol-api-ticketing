using Microsoft.EntityFrameworkCore;
using System.Globalization;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Request;
using XBOL.Ticketing.Data;
using ModelOrder = XBOL.Ticketing.Core.Model.Order;

namespace XBOL.Ticketing.Services.Email;

public class BookingEmailModelBuilder(XBOLDbContext dbContext)
{
    private const string DateTimeFormat = "dddd, d 'de' MMMM 'de' yyyy, h:mm tt";
    private const string DateFormat = "dddd, d 'de' MMMM 'de' yyyy";

    private static readonly List<string> EntryInstructions =
    [
        "Presenta tu código QR en la entrada del evento desde tu dispositivo móvil.",
        "No es necesario imprimir los boletos.",
        "Es obligatorio presentar una identificación oficial.",
        "También puedes descargar tus boletos desde la sección \"Mis Tickets\"."
    ];

    public async Task<OrderEmailModel> BuildAsync(
        long orderId,
        string toAddress,
        string toName,
        string culture = "es-MX",
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Fees)
            .Include(o => o.Client)
            .Include(o => o.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        var cultureInfo = new CultureInfo(culture);

        return order.OrderType switch
        {
            OrderType.Bundle => await BuildBundleEmailModelAsync(order, toAddress, toName, cultureInfo, cancellationToken),
            _ => await BuildEventEmailModelAsync(order, toAddress, toName, cultureInfo, cancellationToken)
        };
    }

    private async Task<OrderEmailModel> BuildEventEmailModelAsync(
        ModelOrder order,
        string toAddress,
        string toName,
        CultureInfo cultureInfo,
        CancellationToken cancellationToken)
    {
        var ticketIds = order.Items
            .Where(i => i.ItemType == ItemType.Ticket)
            .Select(i => i.ItemReferenceId)
            .ToList();

        var tickets = await dbContext.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .Include(t => t.EventSchedule)
                .ThenInclude(es => es.Event)
                    .ThenInclude(e => e!.VenueMap)
                        .ThenInclude(vm => vm!.Venue)
            .Include(t => t.EventSeat)
                .ThenInclude(es => es.BaseSeat)
                    .ThenInclude(bs => bs.BaseRow)
            .Include(t => t.CurrentClient)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var firstTicket = tickets.FirstOrDefault()
            ?? throw new InvalidOperationException($"Order {order.Id} has no tickets");

        var schedule = firstTicket.EventSchedule;
        var @event = schedule.Event;
        var venue = @event.VenueMap?.Venue;

        var eventImage = await GetEventMediaUrlAsync(@event.Id, "Banner", cancellationToken);

        return new OrderEmailModel
        {
            ToAddress = toAddress,
            ToName = toName,
            Culture = cultureInfo.Name,
            Subject = $"Confirmación de orden {order.Reference}",
            EventTitle = @event.Name,
            EventSubtitle = @event.Subtitle ?? "",
            EventImageUrl = eventImage ?? @event.BannerImageUrl ?? "",
            OrderDetails = new OrderDetailsInfo
            {
                OrderNumber = order.Reference,
                Date = schedule.StartDateTime.ToString(DateFormat, cultureInfo),
                Time = schedule.StartDateTime.ToString("h:mm tt", cultureInfo),
                SubTotal = order.SubTotal.ToString("C", cultureInfo),
                Total = order.Total.ToString("C", cultureInfo),
                PaymentMethod = order.Payments.FirstOrDefault()?.PaymentType.ToString() ?? "N/A",
                PurchaseDate = order.CreatedAt.ToString(DateTimeFormat, cultureInfo),
                Venue = new VenueInfo
                {
                    Name = venue?.Name ?? "",
                    Address = venue is null ? "" : $"{venue.AddressLine}, {venue.City}, {venue.State}"
                },
                Fees = order.Fees.Select(f => new OrderFeeInfo
                {
                    Description = f.FeeType,
                    Amount = f.Amount
                }).ToList()
            },
            ClientInfo = new OrderClientInfo
            {
                Name = order.Client.FullName ?? toName,
                Email = order.Client.Email ?? toAddress
            },
            Seats = tickets.Select(t => new SeatInfo
            {
                TicketType = "General",
                SeatKey = t.TicketCode,
                Zone = t.SectionLabelSnapshot,
                Row = t.EventSeat?.BaseSeat?.BaseRow?.RowLabel ?? "",
                Seat = t.EventSeat?.BaseSeat?.SeatNumber ?? t.SeatLabelSnapshot,
                Price = order.Items.FirstOrDefault(i =>
                    i.ItemReferenceId == t.Id && i.ItemType == ItemType.Ticket)?.Price ?? 0m,
                Fees = []
            }).ToList(),
            GoogleWalletUrl = "#",
            AppleWalletUrl = "#",
            EntryInstructions = EntryInstructions,
            PromoBannerImageUrl = "",
            PromoBannerLinkUrl = @event.LandingUrl
        };
    }

    private async Task<OrderEmailModel> BuildBundleEmailModelAsync(
        ModelOrder order,
        string toAddress,
        string toName,
        CultureInfo cultureInfo,
        CancellationToken cancellationToken)
    {
        var passIds = order.Items
            .Where(i => i.ItemType == ItemType.BundlePass)
            .Select(i => i.ItemReferenceId)
            .ToList();

        var passes = await dbContext.BundlePasses
            .Where(sp => passIds.Contains(sp.Id))
            .Include(sp => sp.BundleSeat)
                .ThenInclude(bs => bs!.BaseSeat)
                    .ThenInclude(bs => bs.BaseRow)
                        .ThenInclude(br => br.BaseSection)
            .Include(sp => sp.Bundle)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var firstPass = passes.FirstOrDefault()
            ?? throw new InvalidOperationException($"Order {order.Id} has no bundle passes");

        var bundle = firstPass.Bundle;
        var bundleImage = await GetEventMediaUrlAsync(bundle.Id, "Facade", cancellationToken);

        return new OrderEmailModel
        {
            ToAddress = toAddress,
            ToName = toName,
            Culture = cultureInfo.Name,
            Subject = $"Confirmación de orden {order.Reference}",
            EventTitle = bundle.Name,
            EventSubtitle = "",
            EventImageUrl = bundleImage ?? bundle.BannerImageUrl ?? "",
            OrderDetails = new OrderDetailsInfo
            {
                OrderNumber = order.Reference,
                Date = bundle.StartDate?.ToString(DateFormat, cultureInfo) ?? "",
                Time = bundle.StartDate?.ToString("h:mm tt", cultureInfo) ?? "",
                SubTotal = order.SubTotal.ToString("C", cultureInfo),
                Total = order.Total.ToString("C", cultureInfo),
                PaymentMethod = order.Payments.FirstOrDefault()?.PaymentType.ToString() ?? "N/A",
                PurchaseDate = order.CreatedAt.ToString(DateTimeFormat, cultureInfo),
                Venue = new VenueInfo
                {
                    Name = "",
                    Address = ""
                },
                Fees = order.Fees.Select(f => new OrderFeeInfo
                {
                    Description = f.FeeType,
                    Amount = f.Amount
                }).ToList()
            },
            ClientInfo = new OrderClientInfo
            {
                Name = order.Client.FullName ?? toName,
                Email = order.Client.Email ?? toAddress
            },
            Seats = passes.Select(sp => new SeatInfo
            {
                TicketType = "Bundle Pass",
                SeatKey = sp.TrackingCode,
                Zone = sp.BundleSeat?.BaseSeat?.BaseRow?.BaseSection?.DisplayName ?? "",
                Row = sp.BundleSeat?.BaseSeat?.BaseRow?.RowLabel ?? "",
                Seat = sp.BundleSeat?.BaseSeat?.SeatNumber ?? "",
                Price = order.Items.FirstOrDefault(i =>
                    i.ItemReferenceId == sp.Id && i.ItemType == ItemType.BundlePass)?.Price ?? 0m,
                Fees = []
            }).ToList(),
            GoogleWalletUrl = "#",
            AppleWalletUrl = "#",
            EntryInstructions = EntryInstructions,
            PromoBannerImageUrl = "",
            PromoBannerLinkUrl = bundle.LandingUrl
        };
    }

    private async Task<string?> GetEventMediaUrlAsync(
        long eventId,
        string mediaType,
        CancellationToken cancellationToken)
    {
        return await dbContext.EventMedias
            .Where(m => m.EventId == eventId && m.MediaType == mediaType)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Url)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
