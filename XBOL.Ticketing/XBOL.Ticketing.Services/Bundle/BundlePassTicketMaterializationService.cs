using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using ModelOrder = XBOL.Ticketing.Core.Model.Order;
using ModelTicket = XBOL.Ticketing.Core.Model.Ticket;

namespace XBOL.Ticketing.Services.Bundle;

public class BundlePassTicketMaterializationService(XBOLDbContext dbContext) : IBundlePassTicketMaterializationService
{
    public async Task<int> MaterializeIssuedTicketsAsync(
        long bundleId,
        IReadOnlyCollection<long> eventScheduleIds,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var scheduleIds = eventScheduleIds.Distinct().ToArray();
        if (scheduleIds.Length == 0)
        {
            return 0;
        }

        var passes = await (
                from pass in dbContext.BundlePasses
                    .Include(p => p.Client)
                    .Include(p => p.BundleSeat)
                join item in dbContext.OrderItems on pass.Id equals item.ItemReferenceId
                join order in dbContext.Orders on item.OrderId equals order.Id
                where pass.BundleId == bundleId
                      && pass.Status == BundlePassStatus.Active
                      && item.ItemType == ItemType.BundlePass
                      && order.Status == OrderStatus.Paid
                select new MaterializableBundlePass(pass, order))
            .ToListAsync(cancellationToken);

        if (passes.Count == 0)
        {
            return 0;
        }

        var eventSeats = await dbContext.EventSeats
            .Include(seat => seat.EventSection)
            .Where(seat => scheduleIds.Contains(seat.EventSection.EventScheduleId))
            .ToListAsync(cancellationToken);

        var eventSeatsByScheduleAndBaseSeat = eventSeats
            .GroupBy(seat => (seat.EventSection.EventScheduleId, seat.BaseSeatId))
            .ToDictionary(group => group.Key, group => group.First());

        var eventSeatsByScheduleAndTrackingCode = eventSeats
            .GroupBy(seat => (seat.EventSection.EventScheduleId, seat.ExternalSeatObjectKey))
            .ToDictionary(group => group.Key, group => group.First());

        var inventoryBatchIds = await dbContext.InventoryBatches
            .Where(batch => scheduleIds.Contains(batch.EventScheduleId))
            .GroupBy(batch => batch.EventScheduleId)
            .Select(group => new
            {
                EventScheduleId = group.Key,
                InventoryBatchId = group.OrderBy(batch => batch.Id).Select(batch => batch.Id).First()
            })
            .ToDictionaryAsync(batch => batch.EventScheduleId, batch => batch.InventoryBatchId, cancellationToken);

        var passIds = passes.Select(pass => pass.Pass.Id).ToArray();
        var existingScheduleIdsByPassId = await dbContext.BundlePassEventTickets
            .Where(join => passIds.Contains(join.BundlePassId)
                           && scheduleIds.Contains(join.Ticket.EventScheduleId))
            .Select(join => new
            {
                join.BundlePassId,
                join.Ticket.EventScheduleId
            })
            .ToListAsync(cancellationToken);

        var existing = existingScheduleIdsByPassId
            .GroupBy(join => join.BundlePassId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(join => join.EventScheduleId).ToHashSet());

        var now = DateTimeOffset.UtcNow;
        var created = 0;

        foreach (var pass in passes)
        {
            foreach (var scheduleId in scheduleIds)
            {
                if (existing.TryGetValue(pass.Pass.Id, out var existingScheduleIds)
                    && existingScheduleIds.Contains(scheduleId))
                {
                    continue;
                }

                var eventSeat = ResolveEventSeat(
                    pass.Pass,
                    scheduleId,
                    eventSeatsByScheduleAndBaseSeat,
                    eventSeatsByScheduleAndTrackingCode);

                var ticket = new ModelTicket
                {
                    EventScheduleId = eventSeat.EventSection.EventScheduleId,
                    EventSectionId = eventSeat.EventSectionId,
                    EventSeatId = eventSeat.Id,
                    InventoryBatchId = inventoryBatchIds.TryGetValue(eventSeat.EventSection.EventScheduleId, out var inventoryBatchId)
                        ? inventoryBatchId
                        : null,
                    OriginalClientId = pass.Pass.ClientId,
                    CurrentClientId = pass.Pass.ClientId,
                    OriginalOrder = pass.Order,
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

                dbContext.BundlePassEventTickets.Add(new BundlePassEventTicket
                {
                    BundlePassId = pass.Pass.Id,
                    Ticket = ticket
                });
                created++;
            }
        }

        if (created > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private static EventSeat ResolveEventSeat(
        BundlePass pass,
        long scheduleId,
        IReadOnlyDictionary<(long ScheduleId, long BaseSeatId), EventSeat> eventSeatsByScheduleAndBaseSeat,
        IReadOnlyDictionary<(long ScheduleId, string SeatKey), EventSeat> eventSeatsByScheduleAndTrackingCode)
    {
        if (pass.BundleSeat?.BaseSeatId is { } baseSeatId
            && eventSeatsByScheduleAndBaseSeat.TryGetValue((scheduleId, baseSeatId), out var eventSeat))
        {
            return eventSeat;
        }

        if (eventSeatsByScheduleAndTrackingCode.TryGetValue((scheduleId, pass.TrackingCode), out var fallbackSeat))
        {
            return fallbackSeat;
        }

        throw new InvalidOperationException(
            $"EventSchedule {scheduleId} does not contain seat {pass.TrackingCode} for BundlePass {pass.Id}.");
    }

    private sealed record MaterializableBundlePass(BundlePass Pass, ModelOrder Order);
}
