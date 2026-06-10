using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
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
        SequenceTrackerService sequenceTrackerService) : IBookingOrchestrationService
    {
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
            var bookedSeatKeys = await seatsIoBookingClient.BookSeatsAsync(
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
                    await seatsIoBookingClient.ReleaseBookedSeatsAsync(
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
            ValidateBundleBookingWindow(bundle, now);
            var bundleSeats = ResolveRequestedBundleSeats(bundle, request.Seats.Select(s => s.SeatKey));
            var requestedSeatKeys = bundleSeats.Keys.ToHashSet(StringComparer.Ordinal);
            var client = await ResolveBuyerAsync(request.ClientContact, actorUserId, now, cancellationToken);
            await ValidateBundleRenewalSourceAsync(
                request,
                bundle,
                client,
                requestedSeatKeys,
                cancellationToken);
            var scheduleIds = schedules.Select(s => s.Id).ToArray();
            var eventSeats = await LoadBundleEventSeatsAsync(scheduleIds, requestedSeatKeys, cancellationToken);
            var inventoryBatchIds = await LoadInventoryBatchIdsAsync(scheduleIds, cancellationToken);
            var remoteBookings = new List<RemoteBooking>();

            try
            {
                if (bundle.BundleType == BundleType.SeasonPass)
                {
                    if (string.IsNullOrWhiteSpace(bundle.ExternalKey))
                    {
                        throw new InvalidOperationException("SeasonPass bundle has no Seats.io season key.");
                    }

                    var bookedSeatKeys = await seatsIoBookingClient.BookSeatsAsync(
                        bundle.ExternalKey,
                        request.Seats,
                        request.HoldToken,
                        cancellationToken);
                    remoteBookings.Add(new RemoteBooking(bundle.ExternalKey, bookedSeatKeys));
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

                        var bookedSeatKeys = await seatsIoBookingClient.BookSeatsAsync(
                            schedule.ExternalEventKey,
                            request.Seats,
                            request.HoldToken,
                            cancellationToken);
                        remoteBookings.Add(new RemoteBooking(schedule.ExternalEventKey, bookedSeatKeys));
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
                        await seatsIoBookingClient.ReleaseBookedSeatsAsync(
                            remoteBooking.EventKey,
                            remoteBooking.SeatKeys,
                            cancellationToken);
                    }
                }

                throw;
            }
        }

        private async Task<BookingResultResponse> PersistEventBookingAsync(
            BookSeatsActionRequest request,
            Guid actorUserId,
            IReadOnlyList<string> bookedSeatKeys,
            List<EventSeat> eventSeats,
            CancellationToken cancellationToken)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var client = await ResolveBuyerAsync(request.ClientContact, actorUserId, now, cancellationToken);
            var total = request.PaymentInfoRequest.IsCourtesy ? 0 : request.Seats.Sum(x => x.SeatPrice);
            var order = new ModelOrder
            {
                Client = client,
                UserId = null,
                Reference = await ResolveReference(request, "ORD", request.EventScheduleId),
                SubTotal = total,
                TotalFees = 0,
                TotalTaxes = 0,
                Total = total,
                Status = OrderStatus.Paid,
                OrderType = OrderType.Ticket,
                SaleChannel = SaleChannel.BoxOffice,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId
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
                    PrivateToken = Guid.NewGuid().ToString("N"),
                    SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                    SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,
                    IsDigital = true,
                    PricePaid = request.Seats.First(s => s.SeatKey == eventSeat.ExternalSeatObjectKey).SeatPrice,
                    Status = TicketStatus.Issued,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = actorUserId,
                    UpdatedBy = actorUserId
                });
            }

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var ticket in order.Tickets)
            {
                order.Items.Add(new ModelOrderItem
                {
                    ItemType = ItemType.Ticket,
                    ItemReferenceId = ticket.Id,
                    IsCourtesy = request.PaymentInfoRequest.IsCourtesy,
                    Price = ticket.PricePaid
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var total = request.PaymentInfoRequest.IsCourtesy ? 0 : request.Seats.Sum(x => x.SeatPrice);
            var order = new ModelOrder
            {
                Client = client,
                UserId = null,
                Reference = await ResolveReference(request, "ORD", bundle.Id),
                SubTotal = total,
                TotalFees = 0,
                TotalTaxes = 0,
                Total = total,
                Status = OrderStatus.Paid,
                OrderType = OrderType.Bundle,
                SaleChannel = SaleChannel.BoxOffice,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = actorUserId,
                UpdatedBy = actorUserId,
                RelatedOrderId = request.ReferenceOrderId
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
                dbContext.BundlePasses.Add(pass);
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
                    PrivateToken = Guid.NewGuid().ToString("N"),
                    SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                    SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,
                    IsDigital = true,
                    PricePaid = 0,
                    Status = TicketStatus.Issued,
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

            dbContext.Orders.Add(order);
            dbContext.BundlePassEventTickets.AddRange(joins);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var pass in passesBySeatKey.Values)
            {
                order.Items.Add(new ModelOrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = pass.Id,
                    IsCourtesy = request.PaymentInfoRequest.IsCourtesy,
                    Price = pass.Price
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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

        private async Task<long?> GetInventoryBatchIdAsync(
            long eventScheduleId,
            CancellationToken cancellationToken)
        {
            var inventoryBatch = await dbContext.InventoryBatches
                .Where(b =>
                    b.EventScheduleId == eventScheduleId &&
                    b.Status == InventoryBatchStatus.Active)
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            //if (inventoryBatch is null)
            //{
            //    throw new InvalidOperationException("No active inventory batch exists for this event schedule.");
            //}

            return inventoryBatch?.Id;
        }

        private async Task<Dictionary<long, long>> LoadInventoryBatchIdsAsync(
            IReadOnlyCollection<long> eventScheduleIds,
            CancellationToken cancellationToken)
        {
            var batches = await dbContext.InventoryBatches
                .Where(batch =>
                    eventScheduleIds.Contains(batch.EventScheduleId) &&
                    batch.Status == InventoryBatchStatus.Active)
                .GroupBy(batch => batch.EventScheduleId)
                .Select(group => new { EventScheduleId = group.Key, InventoryBatchId = group.Min(batch => batch.Id) })
                .ToDictionaryAsync(
                    item => item.EventScheduleId,
                    item => item.InventoryBatchId,
                    cancellationToken);

            //var missingScheduleId = eventScheduleIds.FirstOrDefault(id => !batches.ContainsKey(id));
            //if (missingScheduleId != 0)
            //{
            //    throw new InvalidOperationException(
            //        $"No active inventory batch exists for event schedule {missingScheduleId}.");
            //}

            return batches;
        }

        private async Task<List<EventSeat>> LoadRequestedEventSeatsAsync(
            BookSeatsActionRequest request,
            CancellationToken cancellationToken)
        {
            var seatKeys = request.Seats.Select(s => s.SeatKey).ToHashSet(StringComparer.Ordinal);
            var query = dbContext.EventSeats
                .Include(s => s.EventSection)
                .ThenInclude(s => s.EventSchedule)
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
            var eventSeats = await dbContext.EventSeats
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
            var bundle = await dbContext.Bundles
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

        private static void ValidateBundleBookingWindow(ModelBundle bundle, DateTimeOffset now)
        {
            if (bundle.OnSaleDate.HasValue && now < bundle.OnSaleDate.Value)
            {
                throw new InvalidOperationException("Bundle is not on sale.");
            }

            if (bundle.OffSaleDate.HasValue && now > bundle.OffSaleDate.Value)
            {
                throw new InvalidOperationException("Bundle is not on sale.");
            }

            if (!bundle.PreviousBundleId.HasValue)
            {
                return;
            }

            if (bundle.RenewalStartDate.HasValue && now < bundle.RenewalStartDate.Value)
            {
                throw new InvalidOperationException("Bundle renewal window is not open.");
            }

            if (bundle.RenewalEndDate.HasValue && now > bundle.RenewalEndDate.Value)
            {
                throw new InvalidOperationException("Bundle renewal window is not open.");
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
            CancellationToken cancellationToken)
        {
            if (!bundle.PreviousBundleId.HasValue)
            {
                return;
            }

            if (!request.ReferenceOrderId.HasValue)
            {
                throw new InvalidOperationException("ReferenceOrderId is required for renewal bundle booking.");
            }

            var sourceSeatKeys = await (
                    from order in dbContext.Orders.AsNoTracking()
                    from item in order.Items
                    join pass in dbContext.BundlePasses.AsNoTracking()
                        on item.ItemReferenceId equals pass.Id
                    where order.Id == request.ReferenceOrderId.Value
                          && order.OrderType == OrderType.Bundle
                          && order.Status == OrderStatus.Paid
                          && order.ClientId == client.Id
                          && item.ItemType == ItemType.BundlePass
                          && pass.BundleId == bundle.PreviousBundleId.Value
                          && pass.Status == BundlePassStatus.Active
                          && requestedSeatKeys.Contains(pass.TrackingCode)
                    select pass.TrackingCode)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (sourceSeatKeys.Count != requestedSeatKeys.Count)
            {
                throw new InvalidOperationException(
                    "Referenced source order does not own the requested bundle seats.");
            }

            var alreadyRenewed = await dbContext.BundlePasses
                .AsNoTracking()
                .AnyAsync(pass =>
                    pass.BundleId == bundle.Id &&
                    pass.ClientId == client.Id &&
                    requestedSeatKeys.Contains(pass.TrackingCode),
                    cancellationToken);

            if (alreadyRenewed)
            {
                throw new InvalidOperationException("One or more requested bundle seats have already been renewed.");
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
                .Where(schedule => request.EventScheduleId == 0 || schedule.Id == request.EventScheduleId)
                .ToList();

            if (schedules.Count == 0)
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
                client = await dbContext.Clients.FindAsync([contact.Id.Value], cancellationToken);
                if (client is null)
                {
                    throw new KeyNotFoundException($"Client {contact.Id.Value} was not found.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                var email = contact.Email.Trim();
                client = await dbContext.Clients.FirstOrDefaultAsync(
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
                dbContext.Clients.Add(client);
                return client;
            }

            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                client.Email = contact.Email.Trim();
            }

            if (contact.PhoneRegionCodeId.HasValue)
            {
                client.PhoneRegionCodeId = contact.PhoneRegionCodeId;
            }

            if (!string.IsNullOrWhiteSpace(contact.PhoneNumber))
            {
                client.PhoneNumber = NormalizePhoneNumber(contact.PhoneNumber);
            }

            client.FullName = ResolveFullName(contact, client.FullName);
            client.UpdatedAt = now;
            client.UpdatedBy = actorUserId;
            return client;
        }

        private async Task<string> ResolveReference(BookSeatsActionRequest request, string prefix, long referenceId)
        {
            if (!string.IsNullOrWhiteSpace(request.Localizer))
            {
                return request.Localizer;
            }

            return await sequenceTrackerService.GenerateLocalizerAsync(prefix);
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
    }
}
