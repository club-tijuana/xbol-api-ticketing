using ImTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using System.Data;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Options;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Email;
using XBOL.Ticketing.Services.Odasoft.XBOL.Business.Services;
using ModelBundle = XBOL.Ticketing.Core.Model.Bundle;
using ModelBundlePass = XBOL.Ticketing.Core.Model.BundlePass;
using ModelClient = XBOL.Ticketing.Core.Model.Client;
using ModelOrder = XBOL.Ticketing.Core.Model.Order;
using ModelOrderItem = XBOL.Ticketing.Core.Model.OrderItem;
using ModelTicket = XBOL.Ticketing.Core.Model.Ticket;

namespace XBOL.Ticketing.Services.Booking
{
    public class BookingOrchestrationService(
    XBOLDbContext dbContext,
    ISeatsIoBookingClient seatsIoBookingClient,
    SequenceTrackerService sequenceTrackerService,
    BookingConfirmationEmailQueue confirmationEmailQueue,
    ILogger<BookingOrchestrationService> logger,
    IOptions<DefaultExchangeRateOptions> defaultExchangeRateOptions,
    IOptions<PaymentLinkOptions> paymentLinkOptions,
    ExchangeRateRepository exchangeRateRepository) : IBookingOrchestrationService
    {
        private readonly XBOLDbContext _dbContext = dbContext;
        private readonly ISeatsIoBookingClient _seatsIoBookingClient = seatsIoBookingClient;
        private readonly SequenceTrackerService _sequenceTrackerService = sequenceTrackerService;
        private readonly BookingConfirmationEmailQueue _confirmationEmailQueue = confirmationEmailQueue;
        private readonly ILogger<BookingOrchestrationService> _logger = logger;
        private readonly DefaultExchangeRateOptions _defaultExchangeRateOptions = defaultExchangeRateOptions.Value;
        private readonly PaymentLinkOptions _paymentLinkOptions = paymentLinkOptions.Value;
        private readonly ExchangeRateRepository _exchangeRateRepository = exchangeRateRepository;

        public async Task<BookingResultResponse> BookAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            return request.TicketType switch
            {
                ItemType.Ticket => await BookEventAsync(request, actorUserId, cancellationToken),
                ItemType.BundlePass => await BookBundleAsync(request, actorUserId, cancellationToken),
                _ => throw new NotSupportedException($"Booking {request.TicketType} is not supported by this endpoint.")
            };
        }

        private async Task<BookingResultResponse> BookEventAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            var eventSeats = await LoadRequestedEventSeatsAsync(request, cancellationToken);
            var bookedSeatKeys = await _seatsIoBookingClient.BookSeatsAsync(
                request.EventKey,
                request.Seats,
                request.HoldToken,
                cancellationToken);

            try
            {
                return await PersistEventBookingAsync(
                    request,
                    actorUserId,
                    bookedSeatKeys,
                    eventSeats,
                    cancellationToken);
            }
            catch
            {
                if (bookedSeatKeys.Count > 0)
                {
                    await _seatsIoBookingClient.ReleaseBookedSeatsAsync(
                        request.EventKey,
                        bookedSeatKeys,
                        cancellationToken);
                }

                throw;
            }
        }

        private async Task<BookingResultResponse> BookBundleAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken)
        {
            if (!request.BundleId.HasValue)
            {
                throw new InvalidOperationException("BundleId is required for bundle pass booking.");
            }

            var bundle = await LoadBundleAsync(request.BundleId.Value, cancellationToken);
            var schedules = ResolveBundleSchedules(bundle, request);
            var now = DateTimeOffset.UtcNow;

            ValidateBundleBookingWindow(bundle, request, now);

            var bundleSeats = ResolveRequestedBundleSeats(bundle, request.Seats.Select(s => s.SeatKey));
            var requestedSeatKeys = bundleSeats.Keys.ToHashSet(StringComparer.Ordinal);
            var client = await ResolveBuyerAsync(request.ClientContact, actorUserId, now, cancellationToken);
            await ValidateBundleRenewalSourceAsync(
                request,
                bundle,
                client,
                requestedSeatKeys,
                now,
                cancellationToken);
            var scheduleIds = schedules.Select(s => s.Id).ToArray();
            var eventSeats = await LoadBundleEventSeatsAsync(scheduleIds, requestedSeatKeys, cancellationToken);
            var inventoryBatchIds = await LoadInventoryBatchIdsAsync(scheduleIds, cancellationToken);
            var remoteBookings = new List<RemoteBooking>();

            try
            {
                if (bundle.BundleType == BundleType.SeasonPass)
                {
                    var seasonKey = await ValidateSeasonPassRemoteReadinessAsync(
                        bundle,
                        schedules,
                        request,
                        cancellationToken);

                    var bookedSeatKeys = await _seatsIoBookingClient.BookSeatsAsync(
                        seasonKey,
                        request.Seats,
                        request.HoldToken,
                        cancellationToken);
                    remoteBookings.Add(new RemoteBooking(seasonKey, bookedSeatKeys));
                }
                else
                {
                    foreach (var schedule in schedules)
                    {
                        if (string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
                        {
                            throw new InvalidOperationException(
                                $"Bundle schedule {schedule.Id} has no Seats.io event key.");
                        }
                    }

                    string[] eventKeys = schedules
                        .Where(s => !string.IsNullOrWhiteSpace(s.ExternalEventKey))
                        .Select(s => s.ExternalEventKey)
                        .ToArray();

                    var bookedSeatKeys = await _seatsIoBookingClient.BookSeatsAsync(
                        eventKeys,
                        request.Seats,
                        request.HoldToken,
                        cancellationToken);

                    foreach (var eventKey in eventKeys)
                    {
                        remoteBookings.Add(new RemoteBooking(eventKey, bookedSeatKeys));
                    }
                }

                return await PersistBundleBookingAsync(
                    request,
                    actorUserId,
                    bundle,
                    bundleSeats,
                    client,
                    eventSeats,
                    inventoryBatchIds,
                    remoteBookings,
                    cancellationToken);
            }
            catch
            {
                foreach (var remoteBooking in remoteBookings.AsEnumerable().Reverse())
                {
                    if (remoteBooking.SeatKeys.Count > 0)
                    {
                        await _seatsIoBookingClient.ReleaseBookedSeatsAsync(
                            remoteBooking.EventKey,
                            remoteBooking.SeatKeys,
                            cancellationToken);
                    }
                }

                throw;
            }
        }

        private async Task<string> ValidateSeasonPassRemoteReadinessAsync(
            ModelBundle bundle,
            IReadOnlyCollection<EventSchedule> schedules,
            BookSeatsActionRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bundle.ExternalKey))
            {
                throw new InvalidOperationException("SeasonPass bundle has no Seats.io season key.");
            }

            if (!await _seatsIoBookingClient.EventOrSeasonExistsAsync(
                    bundle.ExternalKey,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Seats.io season {bundle.ExternalKey} does not exist for bundle {bundle.Id}.");
            }

            var requestedSeatKeys = request.Seats
                .Select(seat => seat.SeatKey)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (!await _seatsIoBookingClient.ValidateSeatsExistAsync(
                    bundle.ExternalKey,
                    requestedSeatKeys,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Seats.io season {bundle.ExternalKey} does not contain one or more requested seats.");
            }

            return bundle.ExternalKey;
        }

        private async Task<BookingResultResponse> PersistEventBookingAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            IReadOnlyList<string> bookedSeatKeys,
            List<EventSeat> eventSeats,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            bool isCourtesy = request.PaymentInfoRequest.IsCourtesy;
            bool isPaymentLink = request.IsPaymentLink;

            var client = await ResolveBuyerAsync(request.ClientContact, actorUserId, now, cancellationToken);

            EventSeat firstSeat = eventSeats.First();

            OrderTotalBreakDown breakDown = await OrderTotalBreakDownAsync(request, firstSeat.EventSection.EventSchedule.Event.OrganizerId, actorUserId);

            var order = new ModelOrder
            {
                Client = client,
                UserId = actorUserId,
                Reference = await ResolveReference(request, "ORD", request.EventScheduleId),
                SubTotal = breakDown.SubTotal,
                TotalFees = breakDown.Fee,
                TotalTaxes = breakDown.Tax,
                Discount = breakDown.Discount,
                Total = breakDown.Total,
                Status = isPaymentLink ? OrderStatus.Pending : OrderStatus.Paid,
                PaidAt = isPaymentLink ? DateTimeOffset.MaxValue : now,
                OrderType = OrderType.Ticket,
                SaleChannel = SaleChannel.BoxOffice,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId,
                Fees = breakDown.Fees,
                Taxes = breakDown.Taxes,
                Payments = breakDown.Payments,
                OrderTags = isPaymentLink ? [OrderTag.PaymentLink] : []
            };

            var inventoryBatchId = await GetInventoryBatchIdAsync(
                request.EventScheduleId,
                cancellationToken);

            foreach (var eventSeat in eventSeats)
            {
                order.Tickets.Add(new ModelTicket
                {
                    EventScheduleId = eventSeat.EventSection.EventScheduleId,
                    EventSectionId = eventSeat.EventSectionId,
                    EventSeatId = eventSeat.Id,
                    InventoryBatchId = inventoryBatchId,
                    OriginalClient = client,
                    CurrentClient = client,
                    OriginalOrder = order,
                    TicketCode = eventSeat.ExternalSeatObjectKey,
                    TicketType = request.TicketType.ToString(),
                    PrivateToken = isPaymentLink ? null : Guid.NewGuid().ToString("N"),
                    SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                    SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,
                    IsDigital = true,
                    PricePaid = request.Seats.First(s => s.SeatKey == eventSeat.ExternalSeatObjectKey).SeatPrice,
                    Status = isPaymentLink ? TicketStatus.PendingPayment : TicketStatus.Issued,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                });
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var ticket in order.Tickets)
            {
                var seat = request.Seats.Find(x => x.SeatKey == ticket.SeatLabelSnapshot);

                order.Items.Add(new ModelOrderItem
                {
                    ItemType = ItemType.Ticket,
                    ItemReferenceId = ticket.Id,
                    IsCourtesy = request.PaymentInfoRequest.IsCourtesy,
                    Price = breakDown.ItemsPriceList.ContainsKey(seat.PriceListItemId)
                            ? breakDown.ItemsPriceList[seat.PriceListItemId].BasePrice
                            : ticket.PricePaid,
                    PriceListItemId = request.Seats.FirstOrDefault(s => s.SeatKey == ticket.TicketCode)?.PriceListItemId ?? 0
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            LogStartingConfirmationEmailEnqueue("event", order);
            var emailResults = await _confirmationEmailQueue.EnqueueAsync(order.Id, client, cancellationToken);
            LogCompletedConfirmationEmailEnqueue("event", order, emailResults);

            return new BookingResultResponse
            {
                OrderId = order.Id,
                Reference = order.Reference,
                BookedSeatKeys = bookedSeatKeys,
                TicketIds = order.Tickets.Select(t => t.Id).ToList(),
                ClientId = client.Id,
                Total = order.Total
            };
        }

        private async Task<BookingResultResponse> PersistBundleBookingAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            ModelBundle bundle,
            IReadOnlyDictionary<string, BundleSeat> bundleSeats,
            ModelClient client,
            IReadOnlyList<EventSeat> eventSeats,
            IReadOnlyDictionary<long, long> inventoryBatchIds,
            IReadOnlyList<RemoteBooking> remoteBookings,
            CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await EnsureSourceOrderHasNotBeenRenewedAsync(request, bundle, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            bool isCourtesy = request.PaymentInfoRequest.IsCourtesy;
            bool isPaymentLink = request.IsPaymentLink;

            OrderTotalBreakDown breakDown = await OrderTotalBreakDownAsync(request, bundle.OrganizerId, actorUserId);

            var order = new ModelOrder
            {
                Client = client,
                UserId = actorUserId,
                Reference = await ResolveReference(request, "ORD", bundle.Id),
                SubTotal = breakDown.SubTotal,
                TotalFees = breakDown.Fee,
                TotalTaxes = breakDown.Tax,
                Discount = breakDown.Discount,
                Total = breakDown.Total,
                Status = isPaymentLink ? OrderStatus.Pending : OrderStatus.Paid,
                PaidAt = isPaymentLink ? DateTimeOffset.MaxValue : now,
                OrderType = OrderType.Bundle,
                SaleChannel = SaleChannel.BoxOffice,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId,
                RelatedOrderId = request.ReferenceOrderId,
                Payments = breakDown.Payments,
                Fees = breakDown.Fees,
                Taxes = breakDown.Taxes,
                OrderTags = isPaymentLink ? [OrderTag.PaymentLink] : []
            };

            var passesBySeatKey = new Dictionary<string, ModelBundlePass>(StringComparer.Ordinal);

            foreach (var seat in request.Seats)
            {
                var bundleSeat = bundleSeats[seat.SeatKey];
                var pass = new ModelBundlePass
                {
                    BundleId = bundle.Id,
                    Client = client,
                    UserId = null,
                    BundleSeatId = bundleSeat?.Id,
                    TrackingCode = seat.SeatKey,
                    PrivateToken = Guid.NewGuid().ToString("N"),
                    BundlePassType = BundlePassType.Full,
                    Status = BundlePassStatus.Active,
                    IsDigital = true,
                    Price = seat.SeatPrice,
                    PurchasedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                };
                passesBySeatKey[seat.SeatKey] = pass;
                _dbContext.BundlePasses.Add(pass);
            }

            var joins = new List<BundlePassEventTicket>();
            foreach (var eventSeat in eventSeats.OrderBy(s => s.EventSection.EventScheduleId).ThenBy(s => s.ExternalSeatObjectKey))
            {
                var pass = passesBySeatKey[eventSeat.ExternalSeatObjectKey];
                var ticket = new ModelTicket
                {
                    EventScheduleId = eventSeat.EventSection.EventScheduleId,
                    EventSectionId = eventSeat.EventSectionId,
                    EventSeatId = eventSeat.Id,
                    InventoryBatchId = inventoryBatchIds.TryGetValue(eventSeat.EventSection.EventScheduleId, out long value) ? value : null,
                    OriginalClient = client,
                    CurrentClient = client,
                    OriginalOrder = order,
                    TicketCode = eventSeat.ExternalSeatObjectKey,
                    TicketType = ItemType.BundlePass.ToString(),
                    PrivateToken = isPaymentLink ? null : Guid.NewGuid().ToString("N"),
                    SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                    SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,
                    IsDigital = true,
                    PricePaid = 0,
                    Status = isPaymentLink ? TicketStatus.PendingPayment : TicketStatus.Issued,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                };
                order.Tickets.Add(ticket);

                joins.Add(new BundlePassEventTicket
                {
                    BundlePass = pass,
                    Ticket = ticket
                });
            }

            _dbContext.Orders.Add(order);
            _dbContext.BundlePassEventTickets.AddRange(joins);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var pass in passesBySeatKey.Values)
            {
                var seat = request.Seats.Find(x => x.SeatKey == pass.TrackingCode);

                order.Items.Add(new ModelOrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = pass.Id,
                    IsCourtesy = request.PaymentInfoRequest.IsCourtesy,
                    Price = breakDown.ItemsPriceList.ContainsKey(seat.PriceListItemId)
                        ? breakDown.ItemsPriceList[seat.PriceListItemId].BasePrice
                        : pass.Price,
                    PriceListItemId = request.Seats.FirstOrDefault(s => s.SeatKey == pass.TrackingCode)?.PriceListItemId ?? 0
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // We should validate in the request that the client has credit before even processing the payment
            if (request.PaymentInfoRequest.CreditAmount > 0
                && client.ClientCreditAccount != null
                && client.IsActive)
            {
                await CreateClientCreditTransactionAsync(client, request.PaymentInfoRequest.CreditAmount, actorUserId, order.Reference);
            }

            await transaction.CommitAsync(cancellationToken);

            if (!isPaymentLink)
            {
                LogStartingConfirmationEmailEnqueue("bundle", order);
                var emailResults = await _confirmationEmailQueue.EnqueueAsync(order.Id, client, cancellationToken);
                LogCompletedConfirmationEmailEnqueue("bundle", order, emailResults);
            }

            return new BookingResultResponse
            {
                OrderId = order.Id,
                Reference = order.Reference,
                BookedSeatKeys = remoteBookings
                    .SelectMany(booking => booking.SeatKeys)
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                TicketIds = order.Tickets.Select(t => t.Id).ToList(),
                BundlePassIds = passesBySeatKey.Values.Select(pass => pass.Id).ToList(),
                ClientId = client.Id,
                Total = order.Total
            };
        }

        private void LogStartingConfirmationEmailEnqueue(
            string sourcePath,
            ModelOrder order)
        {
            _logger.LogInformation(
                "Starting confirmation email enqueue for {SourcePath} order {OrderId} ({OrderReference}). OrderType={OrderType} SaleChannel={SaleChannel}",
                sourcePath,
                order.Id,
                order.Reference,
                order.OrderType,
                order.SaleChannel);
        }

        private void LogCompletedConfirmationEmailEnqueue(
            string sourcePath,
            ModelOrder order,
            IReadOnlyCollection<BookingConfirmationEmailEnqueueResult> results)
        {
            _logger.LogInformation(
                "Completed confirmation email enqueue for {SourcePath} order {OrderId} ({OrderReference}). Results={EmailEnqueueResults}",
                sourcePath,
                order.Id,
                order.Reference,
                BookingConfirmationEmailEnqueueResultFormatter.Format(results));
        }

        private async Task<long?> GetInventoryBatchIdAsync(
            long eventScheduleId,
            CancellationToken cancellationToken)
        {
            var inventoryBatch = await _dbContext.InventoryBatches
                .Where(b =>
                    b.EventScheduleId == eventScheduleId &&
                    b.Status == InventoryBatchStatus.Active)
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return inventoryBatch?.Id;
        }

        private async Task<Dictionary<long, long>> LoadInventoryBatchIdsAsync(
            IReadOnlyCollection<long> eventScheduleIds,
            CancellationToken cancellationToken)
        {
            var batches = await _dbContext.InventoryBatches
                .Where(batch =>
                    eventScheduleIds.Contains(batch.EventScheduleId) &&
                    batch.Status == InventoryBatchStatus.Active)
                .GroupBy(batch => batch.EventScheduleId)
                .Select(group => new { EventScheduleId = group.Key, InventoryBatchId = group.Min(batch => batch.Id) })
                .ToDictionaryAsync(
                    item => item.EventScheduleId,
                    item => item.InventoryBatchId,
                    cancellationToken);

            return batches;
        }

        private async Task<List<EventSeat>> LoadRequestedEventSeatsAsync(
            BookSeatsActionRequest request,
            CancellationToken cancellationToken)
        {
            var seatKeys = request.Seats.Select(s => s.SeatKey).ToHashSet(StringComparer.Ordinal);
            var query = _dbContext.EventSeats
                .Include(s => s.EventSection)
                    .ThenInclude(s => s.EventSchedule)
                        .ThenInclude(es => es.Event)
                .Where(s =>
                    s.EventSection.EventScheduleId == request.EventScheduleId &&
                    s.EventSection.EventSchedule.ExternalEventKey == request.EventKey &&
                    seatKeys.Contains(s.ExternalSeatObjectKey));

            var eventSeats = await query.ToListAsync(cancellationToken);
            if (eventSeats.Count != seatKeys.Count)
            {
                throw new InvalidOperationException("One or more requested seats do not exist for this event schedule.");
            }

            return eventSeats;
        }

        private async Task<List<EventSeat>> LoadBundleEventSeatsAsync(
            IReadOnlyCollection<long> eventScheduleIds,
            HashSet<string> seatKeys,
            CancellationToken cancellationToken)
        {
            var eventSeats = await _dbContext.EventSeats
                .Include(seat => seat.EventSection)
                .Where(seat =>
                    eventScheduleIds.Contains(seat.EventSection.EventScheduleId) &&
                    seatKeys.Contains(seat.ExternalSeatObjectKey))
                .ToListAsync(cancellationToken);

            var expectedCount = eventScheduleIds.Count * seatKeys.Count;
            if (eventSeats.Count != expectedCount)
            {
                throw new InvalidOperationException("One or more requested bundle seats do not exist for every linked event schedule.");
            }

            return eventSeats;
        }

        private async Task<ModelBundle> LoadBundleAsync(
            long bundleId,
            CancellationToken cancellationToken)
        {
            var bundle = await _dbContext.Bundles
                .Include(b => b.BundleSections)
                .ThenInclude(section => section.BundleSeats)
                .Include(b => b.BundleEventSchedules)
                .ThenInclude(link => link.EventSchedule)
                .FirstOrDefaultAsync(b => b.Id == bundleId, cancellationToken);

            if (bundle is null)
            {
                throw new KeyNotFoundException($"Bundle {bundleId} was not found.");
            }

            if (bundle.Status != EventStatus.Published)
            {
                throw new InvalidOperationException("Bundle must be published before booking.");
            }

            return bundle;
        }

        private static void ValidateBundleBookingWindow(
            ModelBundle bundle,
            BookSeatsActionRequest request,
            DateTimeOffset now)
        {
            if (request.OverrideSaleWindow)
            {
                return;
            }

            if (!bundle.OnSaleDate.HasValue || !bundle.OffSaleDate.HasValue)
            {
                throw new InvalidOperationException("Bundle sale window is not configured.");
            }

            if (now >= bundle.OffSaleDate.Value)
            {
                throw new InvalidOperationException("Bundle is no longer on sale.");
            }

            if (bundle.PreviousBundleId.HasValue)
            {
                if (!bundle.RenewalStartDate.HasValue || !bundle.RenewalEndDate.HasValue)
                {
                    throw new InvalidOperationException("Bundle renewal window is not configured.");
                }

                if (request.ReferenceOrderId.HasValue)
                {
                    if (now < bundle.RenewalStartDate.Value)
                    {
                        throw new InvalidOperationException("Bundle renewal window is not open.");
                    }

                    return;
                }

                if (now < bundle.RenewalEndDate.Value)
                {
                    throw new InvalidOperationException("Bundle is reserved for renewals until the renewal window closes.");
                }
            }

            if (now < bundle.OnSaleDate.Value)
            {
                throw new InvalidOperationException("Bundle is not on sale.");
            }
        }

        private static IReadOnlyDictionary<string, BundleSeat> ResolveRequestedBundleSeats(
            ModelBundle bundle,
            IEnumerable<string> requestedSeatKeys)
        {
            var bundleSeats = bundle.BundleSections
                .SelectMany(section => section.BundleSeats)
                .GroupBy(seat => seat.ExternalSeatObjectKey)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var requestedKeys = requestedSeatKeys.ToArray();
            var missingSeatKeys = requestedKeys
                .Where(seatKey => !bundleSeats.ContainsKey(seatKey))
                .ToArray();

            if (missingSeatKeys.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Bundle seat(s) {string.Join(", ", missingSeatKeys)} are not configured for this bundle.");
            }

            var unavailableSeatKeys = requestedKeys
                .Where(seatKey => !bundleSeats[seatKey].ForSale)
                .ToArray();

            if (unavailableSeatKeys.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Bundle seat(s) {string.Join(", ", unavailableSeatKeys)} are not for sale.");
            }

            return requestedKeys.ToDictionary(
                seatKey => seatKey,
                seatKey => bundleSeats[seatKey],
                StringComparer.Ordinal);
        }

        private async Task ValidateBundleRenewalSourceAsync(
            BookSeatsActionRequest request,
            ModelBundle bundle,
            ModelClient client,
            HashSet<string> requestedSeatKeys,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (!bundle.PreviousBundleId.HasValue || !request.ReferenceOrderId.HasValue)
            {
                return;
            }

            var sourceSeatKeys = await (
                    from order in _dbContext.Orders.AsNoTracking()
                    from item in order.Items
                    join pass in _dbContext.BundlePasses.AsNoTracking()
                        on item.ItemReferenceId equals pass.Id
                    where order.Id == request.ReferenceOrderId.Value
                          && order.OrderType == OrderType.Bundle
                          && order.Status == OrderStatus.Paid
                          && order.ClientId == client.Id
                          && item.ItemType == ItemType.BundlePass
                          && pass.BundleId == bundle.PreviousBundleId.Value
                          && pass.Status == BundlePassStatus.Active
                    select pass.TrackingCode)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (sourceSeatKeys.Count == 0)
            {
                throw new InvalidOperationException(
                    "Referenced source order does not own the requested bundle seats.");
            }

            await EnsureSourceOrderHasNotBeenRenewedAsync(request, bundle, cancellationToken);

            var isProtectedRenewalWindow = bundle.RenewalEndDate.HasValue && now < bundle.RenewalEndDate.Value;
            if (isProtectedRenewalWindow)
            {
                var ownedRequestedSeatCount = sourceSeatKeys
                    .Count(requestedSeatKeys.Contains);

                if (ownedRequestedSeatCount != requestedSeatKeys.Count)
                {
                    throw new InvalidOperationException(
                        "Referenced source order does not own the requested bundle seats.");
                }

                return;
            }

            if (requestedSeatKeys.Count > sourceSeatKeys.Count)
            {
                throw new InvalidOperationException(
                    "Requested bundle seat count exceeds referenced source order entitlement.");
            }

            await EnsureSourceOrderHasNotBeenRenewedAsync(request, bundle, cancellationToken);
        }

        private async Task EnsureSourceOrderHasNotBeenRenewedAsync(
            BookSeatsActionRequest request,
            ModelBundle bundle,
            CancellationToken cancellationToken)
        {
            if (!bundle.PreviousBundleId.HasValue || !request.ReferenceOrderId.HasValue)
            {
                return;
            }

            var originalPassCount = await _dbContext.Orders
                .AsNoTracking()
                .Where(order => order.Id == request.ReferenceOrderId.Value)
                .SelectMany(order => order.Items)
                .CountAsync(item =>
                    item.ItemType == ItemType.BundlePass &&
                    _dbContext.BundlePasses.Any(pass => pass.Id == item.ItemReferenceId),
                    cancellationToken);

            var relatedPassCount = await _dbContext.Orders
                .AsNoTracking()
                .Where(order =>
                    order.RelatedOrderId == request.ReferenceOrderId.Value &&
                    order.OrderType == OrderType.Bundle &&
                    order.Status == OrderStatus.Paid)
                .SelectMany(order => order.Items)
                .CountAsync(item =>
                        item.ItemType == ItemType.BundlePass &&
                        _dbContext.BundlePasses.Any(pass =>
                            pass.Id == item.ItemReferenceId &&
                        pass.BundleId == bundle.Id),
                    cancellationToken);

            var sourceOrderAlreadyRenewed = originalPassCount > 0 && originalPassCount == relatedPassCount;

            if (sourceOrderAlreadyRenewed)
            {
                throw new InvalidOperationException("Referenced source order has already been renewed.");
            }
        }

        private static List<EventSchedule> ResolveBundleSchedules(
            ModelBundle bundle,
            BookSeatsActionRequest request)
        {
            var schedules = bundle.BundleEventSchedules
                .OrderBy(link => link.SortOrder ?? int.MaxValue)
                .ThenBy(link => link.EventScheduleId)
                .Select(link => link.EventSchedule)
                .Where(schedule =>
                    bundle.BundleType == BundleType.SeasonPass ||
                    request.EventScheduleId == 0 ||
                    schedule.Id == request.EventScheduleId)
                .ToList();

            if (schedules.Count == 0 && bundle.BundleType != BundleType.SeasonPass)
            {
                throw new InvalidOperationException("Bundle has no matching event schedules to book.");
            }

            return schedules;
        }

        private async Task<ModelClient> ResolveBuyerAsync(
            ClientInfoRequest contact,
            Guid actorUserId,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ModelClient? client = null;
            if (contact.Id.HasValue)
            {
                client = await _dbContext.Clients
                                .Include(c => c.ClientCreditAccount)
                                .Where(c => c.Id == contact.Id.Value)
                                .SingleOrDefaultAsync(cancellationToken);

                if (client is null)
                {
                    throw new KeyNotFoundException($"Client {contact.Id.Value} was not found.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                var email = contact.Email.Trim();
                client = await _dbContext.Clients.FirstOrDefaultAsync(
                    c => c.Email == email,
                    cancellationToken);
            }

            if (client is null)
            {
                client = new ModelClient
                {
                    ClientType = ClientType.Individual,
                    Email = contact.Email.Trim(),
                    PhoneRegionCodeId = contact.PhoneRegionCodeId,
                    PhoneNumber = NormalizePhoneNumber(contact.PhoneNumber),
                    FullName = ResolveFullName(contact),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                };

                _dbContext.Clients.Add(client);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return client;
            }

            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                client.Email = contact.Email.Trim();
            }

            client.PhoneRegionCodeId = contact.PhoneRegionCodeId;
            client.PhoneNumber = NormalizePhoneNumber(contact.PhoneNumber);
            client.FullName = ResolveFullName(contact, client.FullName);
            client.UpdatedAt = now;
            client.UpdatedBy = actorUserId;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return client;
        }

        private async Task<string> ResolveReference(BookSeatsActionRequest request, string prefix, long referenceId)
        {
            if (!string.IsNullOrWhiteSpace(request.Localizer))
            {
                return request.Localizer;
            }

            return await _sequenceTrackerService.GenerateLocalizerAsync(prefix);
        }

        private static string? ResolveFullName(ClientInfoRequest contact, string? fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(contact.FullName))
            {
                return contact.FullName.Trim();
            }

            var composed = $"{contact.FirstName} {contact.LastName}".Trim();
            return string.IsNullOrWhiteSpace(composed) ? fallback : composed;
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            return new string(phoneNumber.Where(char.IsAsciiDigit).ToArray());
        }

        private sealed record RemoteBooking(string EventKey, IReadOnlyList<string> SeatKeys);

        private async Task<OrderTotalBreakDown> OrderTotalBreakDownAsync(BookSeatsActionRequest request, long? organizerId, Guid actorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool isCourtesy = request.PaymentInfoRequest.IsCourtesy;
            bool isPaymentLink = request.IsPaymentLink;

            ExchangeRateResponse defaultRate = new()
            {
                Id = 0,
                Rate = _defaultExchangeRateOptions.Value
            };

            ExchangeRateResponse currentExchangeRate = defaultRate;

            List<long> priceListIds = request.Seats.Select(s => s.PriceListItemId).Distinct().ToList();

            Dictionary<long, PriceListItem> itemPriceDictionary = await _dbContext.PriceListItems
                                                                        .Include(p => p.FeeList)
                                                                        .AsNoTracking()
                                                                        .Where(p => priceListIds.Contains(p.Id))
                                                                        .ToDictionaryAsync(p => p.Id);

            decimal subTotal = request.Seats
                                .Sum(seat => itemPriceDictionary.ContainsKey(seat.PriceListItemId)
                                    ? itemPriceDictionary[seat.PriceListItemId].BasePrice
                                    : seat.SeatPrice);

            var allFeeItems = request.Seats
                .Where(s => itemPriceDictionary.ContainsKey(s.PriceListItemId))
                .SelectMany(seat => itemPriceDictionary[seat.PriceListItemId].FeeList)
                .ToList();

            decimal fee = allFeeItems.Where(f => f.ChargeCategory != "Tax").Sum(f => f.FeeAmount);
            decimal tax = allFeeItems.Where(f => f.ChargeCategory == "Tax").Sum(f => f.FeeAmount);
            decimal discount = isCourtesy ? (subTotal + fee + tax) : 0;
            decimal total = (subTotal + fee + tax) - discount;

            PaymentInfoRequest paymentInfo = request.PaymentInfoRequest;

            List<Payment> payments = [];

            if (!isPaymentLink)
            {
                bool hasPayments = paymentInfo == null ? false :
                    (
                        paymentInfo.CardAmount > 0
                        || paymentInfo.CreditAmount > 0
                        || paymentInfo.CashAmount > 0
                        || paymentInfo.DolarAmount > 0
                        || paymentInfo.OtherAmount > 0
                    );

                if (!isCourtesy && hasPayments)
                {
                    (payments, currentExchangeRate) = await PaymentInfoToPaymentsAsync(
                        request.PaymentInfoRequest,
                        total,
                        organizerId,
                        CurrencyType.MXN,
                        defaultRate
                    );
                }
                else if (!isCourtesy && !hasPayments)
                {
                    throw new Exception("No payments have been specified.");
                }
                else if (isCourtesy)
                {
                    payments.Add(new Payment
                    {
                        Currency = CurrencyType.MXN,
                        Amount = 0,
                        AmountMXN = 0,
                        ReceivedAmount = 0,
                        ReceivedAmountMXN = 0,
                        ExchangeRateId = 0,
                        ExchangeRate = 0,
                        PaymentType = PaymentType.Courtesy,
                        PaymentStatus = PaymentStatus.Captured,
                        Provider = "",
                        ProviderReference = "",
                        TransactionReference = Guid.Empty,
                        AppliedAt = now,
                        CreatedAt = now,
                        CreatedBy = actorUserId,
                        UpdatedBy = actorUserId,
                    });
                }
            }

            List<OrderFee> fees = [];
            fees.Add(new OrderFee { FeeType = "Comisiones", Amount = fee });

            List<OrderTax> taxes = [];
            taxes.Add(new OrderTax { TaxType = "Impuestos", Amount = tax });

            return new OrderTotalBreakDown(
                SubTotal: subTotal,
                Fee: fee,
                Tax: tax,
                Discount: discount,
                Total: total,
                Fees: fees,
                Taxes: taxes,
                Payments: payments,
                ItemsPriceList: itemPriceDictionary
            );
        }

        private async Task<(List<Payment> Payments, ExchangeRateResponse CurrentExchangeRate)> PaymentInfoToPaymentsAsync(
            PaymentInfoRequest paymentInfo,
            decimal Total,
            long? organizerId,
            CurrencyType currencyType,
            ExchangeRateResponse defaultExchangeRate)
        {
            List<Payment> payments = [];
            DateTimeOffset now = DateTimeOffset.UtcNow;

            decimal cardAmount = paymentInfo.CardAmount;
            decimal creditAmount = paymentInfo.CreditAmount;
            decimal cashAmount = paymentInfo.CashAmount;
            decimal dolarAmount = paymentInfo.DolarAmount;
            decimal otherAmount = paymentInfo.OtherAmount;

            decimal pendingToCharge = Total;

            ExchangeRateResponse? currentExchangeRate = null;
            if (organizerId.HasValue)
            {
                currentExchangeRate = await _exchangeRateRepository.Get(
                    filter: er => er.OrganizerId == organizerId
                    && er.StartedAt <= now
                )
                .OrderByDescending(er => er.StartedAt)
                .Select(er => new ExchangeRateResponse
                {
                    Id = er.Id,
                    Rate = er.Rate
                })
                .FirstOrDefaultAsync();
            }

            currentExchangeRate ??= defaultExchangeRate;

            decimal dolarAmountToMXN = dolarAmount * currentExchangeRate.Rate;
            decimal totalPaid = cardAmount + creditAmount + cashAmount + dolarAmountToMXN + otherAmount;

            if (totalPaid < Total)
            {
                throw new Exception("The payment amount is insufficient to pay for the order.");
            }

            if (cardAmount > 0)
            {
                decimal appliedAmount = Math.Min(cardAmount, Math.Max(0, pendingToCharge));

                payments.Add(new Payment
                {
                    Currency = currencyType,
                    Amount = appliedAmount,
                    AmountMXN = currencyType == CurrencyType.MXN ? appliedAmount : (appliedAmount * currentExchangeRate.Rate),
                    ReceivedAmount = cardAmount,
                    ReceivedAmountMXN = currencyType == CurrencyType.MXN ? cardAmount : (cardAmount * currentExchangeRate.Rate),
                    ExchangeRateId = currentExchangeRate.Id,
                    ExchangeRate = currentExchangeRate.Rate,
                    PaymentType = PaymentType.Card,
                    PaymentStatus = PaymentStatus.Captured,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                });

                pendingToCharge -= appliedAmount;
            }

            if (creditAmount > 0)
            {
                decimal appliedAmount = Math.Min(creditAmount, Math.Max(0, pendingToCharge));

                payments.Add(new Payment
                {
                    Currency = currencyType,
                    Amount = appliedAmount,
                    AmountMXN = currencyType == CurrencyType.MXN ? appliedAmount : (appliedAmount * currentExchangeRate.Rate),
                    ReceivedAmount = creditAmount,
                    ReceivedAmountMXN = currencyType == CurrencyType.MXN ? creditAmount : (creditAmount * currentExchangeRate.Rate),
                    ExchangeRateId = currentExchangeRate.Id,
                    ExchangeRate = currentExchangeRate.Rate,
                    PaymentType = PaymentType.ClientCredit,
                    PaymentStatus = PaymentStatus.Captured,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                });

                pendingToCharge -= appliedAmount;
            }

            if (cashAmount > 0)
            {
                decimal appliedAmount = Math.Min(cashAmount, Math.Max(0, pendingToCharge));

                payments.Add(new Payment
                {
                    Currency = currencyType,
                    Amount = appliedAmount,
                    AmountMXN = currencyType == CurrencyType.MXN ? appliedAmount : (appliedAmount * currentExchangeRate.Rate),
                    ReceivedAmount = cashAmount,
                    ReceivedAmountMXN = currencyType == CurrencyType.MXN ? cashAmount : (cashAmount * currentExchangeRate.Rate),
                    ExchangeRateId = currentExchangeRate.Id,
                    ExchangeRate = currentExchangeRate.Rate,
                    PaymentType = PaymentType.Cash,
                    PaymentStatus = PaymentStatus.Captured,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                });

                pendingToCharge -= appliedAmount;
            }

            if (dolarAmount > 0)
            {
                decimal appliedAmountBase = Math.Min(dolarAmountToMXN, Math.Max(0, pendingToCharge));
                decimal appliedAmountUSD = currentExchangeRate.Rate > 0 ? appliedAmountBase / currentExchangeRate.Rate : 0;

                payments.Add(new Payment
                {
                    Currency = CurrencyType.USD,
                    Amount = appliedAmountUSD,
                    AmountMXN = appliedAmountBase,
                    ReceivedAmount = dolarAmount,
                    ReceivedAmountMXN = dolarAmountToMXN,
                    ExchangeRateId = currentExchangeRate.Id,
                    ExchangeRate = currentExchangeRate.Rate,
                    PaymentType = PaymentType.Cash,
                    PaymentStatus = PaymentStatus.Captured,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                });

                pendingToCharge -= appliedAmountBase;
            }

            if (otherAmount > 0)
            {
                decimal appliedAmount = Math.Min(otherAmount, Math.Max(0, pendingToCharge));

                payments.Add(new Payment
                {
                    Currency = currencyType,
                    Amount = appliedAmount,
                    AmountMXN = currencyType == CurrencyType.MXN ? appliedAmount : (appliedAmount * currentExchangeRate.Rate),
                    ReceivedAmount = otherAmount,
                    ReceivedAmountMXN = currencyType == CurrencyType.MXN ? otherAmount : (otherAmount * currentExchangeRate.Rate),
                    ExchangeRateId = currentExchangeRate.Id,
                    ExchangeRate = currentExchangeRate.Rate,
                    PaymentType = PaymentType.Other,
                    PaymentStatus = PaymentStatus.Captured,
                    Provider = "",
                    ProviderReference = "",
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                });

                pendingToCharge -= appliedAmount;
            }

            return (payments, currentExchangeRate);
        }

        private async Task CreateClientCreditTransactionAsync(ModelClient client, decimal amount, Guid userId, string orderReference)
        {
            _dbContext.ClientCreditTransactions.Add(new ClientCreditTransaction
            {
                ClientCreditAccountId = client.ClientCreditAccount.Id,
                Amount = amount,
                PaymentType = PaymentType.ClientCredit,
                TransactionDate = DateTime.UtcNow.ToUniversalTime(),
                TransactionType = CreditTransactionType.Drawdown,
                Description = orderReference,
                ReferenceId = await _sequenceTrackerService.GenerateLocalizerAsync("CCT"),
                OrderReference = orderReference,
                CreatedAt = DateTime.UtcNow.ToUniversalTime(),
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow.ToUniversalTime(),
                UpdatedBy = userId
            });

            await _dbContext.SaveChangesAsync();
        }

        private sealed record OrderTotalBreakDown(decimal SubTotal, decimal Fee, decimal Tax, decimal Discount, decimal Total, List<OrderFee> Fees, List<OrderTax> Taxes, List<Payment> Payments, Dictionary<long, PriceListItem> ItemsPriceList);
    }
}
