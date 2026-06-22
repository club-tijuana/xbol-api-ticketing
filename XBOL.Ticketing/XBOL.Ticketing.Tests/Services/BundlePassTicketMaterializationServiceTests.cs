using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.Tests.Services;

public class BundlePassTicketMaterializationServiceTests
{
    [Fact]
    public async Task MaterializeIssuedTicketsAsync_CreatesTicketsForPaidPassesAndSkipsPendingOrders()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedMaterializationScenarioAsync(context);

        var sut = new BundlePassTicketMaterializationService(context);

        var created = await sut.MaterializeIssuedTicketsAsync(20, [201], Guid.Empty);
        var createdAgain = await sut.MaterializeIssuedTicketsAsync(20, [201], Guid.Empty);

        created.Should().Be(1);
        createdAgain.Should().Be(0);

        var tickets = await context.Tickets.ToListAsync();
        tickets.Should().ContainSingle();
        tickets[0].EventScheduleId.Should().Be(201);
        tickets[0].TicketCode.Should().Be("A-1");
        tickets[0].Status.Should().Be(TicketStatus.Issued);
        tickets[0].PrivateToken.Should().NotBeNullOrWhiteSpace();
        tickets[0].TicketType.Should().Be(ItemType.BundlePass.ToString());

        var joins = await context.BundlePassEventTickets.ToListAsync();
        joins.Should().ContainSingle(join => join.BundlePassId == 1 && join.TicketId == tickets[0].Id);
        joins.Should().NotContain(join => join.BundlePassId == 2);
    }

    private static async Task SeedMaterializationScenarioAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var phoneRegionCode = new PhoneRegionCode
        {
            Id = 1,
            RegionCode = "MX",
            DialCode = "+52",
            FlagEmoji = "MX"
        };
        var client = new Client
        {
            Id = 1,
            ClientType = ClientType.Individual,
            FullName = "Season Buyer",
            Email = "season@example.test",
            PhoneRegionCode = phoneRegionCode,
            PhoneNumber = "5550000001",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var venue = new Venue
        {
            Id = 1,
            Name = "Main Venue",
            AddressLine = "123 Main",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            Category = VenueCategory.Stadium,
            ShortDescription = "Main",
            LongDescription = "Main venue",
            LogoImageUrl = string.Empty,
            BannerImageUrl = string.Empty,
            LandingUrl = string.Empty,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var venueMap = new VenueMap
        {
            Id = 1,
            Venue = venue,
            Name = "Main chart",
            ExternalMapKey = "chart-main"
        };
        var baseZone = new BaseZone
        {
            Id = 1,
            VenueMap = venueMap,
            Name = "Lower"
        };
        var baseSection = new BaseSection
        {
            Id = 1,
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General
        };
        var baseRow = new BaseRow
        {
            Id = 1,
            BaseSection = baseSection,
            RowLabel = "A",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseSeat = new BaseSeat
        {
            Id = 1,
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard,
            CreatedAt = now,
            UpdatedAt = now
        };
        var bundle = new Bundle
        {
            Id = 20,
            VenueMap = venueMap,
            Name = "Season 2026",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single,
            ExternalKey = "season-20",
            CreatedAt = now,
            UpdatedAt = now
        };
        var bundleSection = new BundleSection
        {
            Id = 1,
            Bundle = bundle,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        var bundleSeat = new BundleSeat
        {
            Id = 1,
            BundleSection = bundleSection,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1",
            ForSale = true
        };
        var eventEntity = new Event
        {
            Id = 30,
            VenueMap = venueMap,
            Name = "Opening Match",
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now
        };
        var schedule = new EventSchedule
        {
            Id = 201,
            Event = eventEntity,
            Status = ScheduleStatus.OnSale,
            ExternalEventKey = "season-20-schedule-201",
            StartDateTime = now.AddDays(10),
            EndDateTime = now.AddDays(10).AddHours(2),
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(9)
        };
        var eventSection = new EventSection
        {
            Id = 1,
            EventSchedule = schedule,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        var eventSeat = new EventSeat
        {
            Id = 1,
            EventSection = eventSection,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1",
            ForSale = true
        };
        var paidPass = new BundlePass
        {
            Id = 1,
            Bundle = bundle,
            Client = client,
            BundleSeat = bundleSeat,
            TrackingCode = "A-1",
            PrivateToken = "paid-pass-token",
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 500m,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        var pendingPass = new BundlePass
        {
            Id = 2,
            Bundle = bundle,
            Client = client,
            BundleSeat = bundleSeat,
            TrackingCode = "A-1",
            PrivateToken = "pending-pass-token",
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 500m,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        var paidOrder = BuildOrder(1, client, "ORD-PAID", OrderStatus.Paid, now);
        paidOrder.Items.Add(new OrderItem
        {
            ItemType = ItemType.BundlePass,
            ItemReferenceId = paidPass.Id,
            Price = 500m
        });
        var pendingOrder = BuildOrder(2, client, "ORD-PENDING", OrderStatus.Pending, now);
        pendingOrder.Items.Add(new OrderItem
        {
            ItemType = ItemType.BundlePass,
            ItemReferenceId = pendingPass.Id,
            Price = 500m
        });

        context.AddRange(
            phoneRegionCode,
            client,
            venue,
            venueMap,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            bundle,
            bundleSection,
            bundleSeat,
            eventEntity,
            schedule,
            eventSection,
            eventSeat,
            paidPass,
            pendingPass,
            paidOrder,
            pendingOrder);
        await context.SaveChangesAsync();
    }

    private static Order BuildOrder(
        long id,
        Client client,
        string reference,
        OrderStatus status,
        DateTimeOffset now)
    {
        return new Order
        {
            Id = id,
            Client = client,
            Reference = reference,
            SubTotal = 500m,
            TotalFees = 0,
            TotalTaxes = 0,
            Discount = 0,
            Total = 500m,
            Status = status,
            OrderType = OrderType.Bundle,
            SaleChannel = SaleChannel.BoxOffice,
            PaidAt = status == OrderStatus.Paid ? now : DateTimeOffset.MaxValue,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
