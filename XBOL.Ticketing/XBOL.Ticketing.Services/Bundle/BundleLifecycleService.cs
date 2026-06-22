using Wolverine;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Services.Bundle;

public class BundleLifecycleService(
    IBundleRepository bundleRepository,
    IMessageBus bus,
    IEventScheduleLifecycleService eventScheduleLifecycleService)
    : IBundleLifecycleService
{
    public async Task PublishAsync(long bundleId, Guid userId, CancellationToken cancellation = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(bundleId)
            ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

        EventStatusTransitions.ValidateTransition(bundle.Status, EventStatus.Published);

        if (bundle.BundleType == BundleType.SeasonPass)
        {
            await bus.InvokeAsync(new CreateSeatsIoSeasonCommand(bundleId, userId), cancellation);
            return;
        }

        var bundleWithSchedules = await bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(bundleId)
            ?? bundle;

        await PublishMissingLinkedSchedulesAsync(bundleWithSchedules, userId, cancellation);

        bundleWithSchedules.Status = EventStatus.Published;
        bundleWithSchedules.PublishedDate ??= DateTimeOffset.UtcNow;
        bundleWithSchedules.ExternalKey = null;
        await bundleRepository.UpdateAsync(bundleWithSchedules);
    }

    public async Task CancelAsync(long bundleId, Guid userId, CancellationToken cancellation = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(bundleId)
            ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

        EventStatusTransitions.ValidateTransition(bundle.Status, EventStatus.Cancelled);

        if (bundle.BundleType == BundleType.SeasonPass)
        {
            await bus.InvokeAsync(new DeleteSeatsIoSeasonCommand(bundleId, userId), cancellation);
            return;
        }

        bundle.Status = EventStatus.Cancelled;
        bundle.ExternalKey = null;
        bundle.UpdatedAt = DateTimeOffset.UtcNow;
        bundle.UpdatedBy = userId;
        await bundleRepository.UpdateAsync(bundle);
    }

    public async Task SyncMetadataAsync(long bundleId, CancellationToken cancellation = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(bundleId)
            ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass ||
            bundle.Status != EventStatus.Published ||
            string.IsNullOrWhiteSpace(bundle.ExternalKey))
        {
            return;
        }

        await bus.InvokeAsync(new UpdateSeatsIoSeasonCommand(bundleId), cancellation);
    }

    public async Task AddSchedulesAsync(
        long bundleId,
        IReadOnlyCollection<long> eventScheduleIds,
        CancellationToken cancellation = default)
    {
        var bundle = await bundleRepository.GetByIdAsync(bundleId)
            ?? throw new KeyNotFoundException($"Bundle {bundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass)
        {
            if (bundle.Status == EventStatus.Published)
            {
                var bundleWithSchedules = await bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(bundleId)
                    ?? bundle;

                await PublishMissingLinkedSchedulesAsync(
                    bundleWithSchedules,
                    Guid.Empty,
                    cancellation,
                    eventScheduleIds);

                if (!string.IsNullOrWhiteSpace(bundleWithSchedules.ExternalKey))
                {
                    bundleWithSchedules.ExternalKey = null;
                    await bundleRepository.UpdateAsync(bundleWithSchedules);
                }
            }

            return;
        }

        if (bundle.Status != EventStatus.Published)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(bundle.ExternalKey))
        {
            throw new InvalidOperationException($"Bundle {bundle.Id} has no Seats.io season key.");
        }

        await bus.InvokeAsync(new AddEventsToSeasonCommand(bundleId, eventScheduleIds.ToArray(), Guid.Empty), cancellation);
    }

    private async Task PublishMissingLinkedSchedulesAsync(
        Core.Model.Bundle bundle,
        Guid userId,
        CancellationToken cancellation,
        IReadOnlyCollection<long>? eventScheduleIds = null)
    {
        var eventScheduleIdSet = eventScheduleIds?.ToHashSet();
        foreach (var link in bundle.BundleEventSchedules.OrderBy(link => link.SortOrder ?? int.MaxValue)
                     .ThenBy(link => link.EventScheduleId))
        {
            if (eventScheduleIdSet is not null && !eventScheduleIdSet.Contains(link.EventScheduleId))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(link.EventSchedule?.ExternalEventKey))
            {
                await eventScheduleLifecycleService.PublishAsync(link.EventScheduleId, userId, cancellation);
            }
        }
    }
}
