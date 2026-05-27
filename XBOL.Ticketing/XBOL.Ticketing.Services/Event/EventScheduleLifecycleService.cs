using Wolverine;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Services.Event;

public class EventScheduleLifecycleService(
    IEventScheduleRepository eventScheduleRepository,
    IBundleEventScheduleRepository bundleEventScheduleRepository,
    IMessageBus bus)
{
    public async Task PublishAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default)
    {
        var schedule = await LoadScheduleAsync(eventScheduleId);
        await EnsureNotSeasonPassOwnedAsync(eventScheduleId);

        ScheduleStatusTransitions.ValidateTransition(schedule.Status, ScheduleStatus.OnSale);
        ScheduleStatusTransitions.ValidateCanGoOnSale(schedule.Event.Status);

        if (string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            await bus.InvokeAsync(new CreateSeatsIoEventCommand(eventScheduleId, userId), cancellation);
            return;
        }

        schedule.Status = ScheduleStatus.OnSale;
        schedule.PublishedDate ??= DateTimeOffset.UtcNow;
        await eventScheduleRepository.UpdateAsync(schedule);
    }

    public async Task SyncMetadataAsync(long eventScheduleId, CancellationToken cancellation = default)
    {
        var schedule = await LoadScheduleAsync(eventScheduleId);

        if (string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            return;
        }

        if (await IsSeasonPassOwnedAsync(eventScheduleId))
        {
            return;
        }

        await bus.InvokeAsync(new UpdateSeatsIoEventCommand(eventScheduleId), cancellation);
    }

    public async Task CancelAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default)
    {
        var schedule = await LoadScheduleAsync(eventScheduleId);
        await EnsureNotSeasonPassOwnedAsync(eventScheduleId);

        if (!string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            await bus.InvokeAsync(
                new DeleteSeatsIoEventCommand(eventScheduleId, userId, SeatsIoEventDeletionMode.Close),
                cancellation);
            return;
        }

        ScheduleStatusTransitions.ValidateTransition(schedule.Status, ScheduleStatus.Closed);
        schedule.Status = ScheduleStatus.Closed;
        await eventScheduleRepository.UpdateAsync(schedule);
    }

    public async Task DeleteAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default)
    {
        var schedule = await LoadScheduleAsync(eventScheduleId);
        await EnsureNotSeasonPassOwnedAsync(eventScheduleId);

        if (!string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            await bus.InvokeAsync(
                new DeleteSeatsIoEventCommand(eventScheduleId, userId, SeatsIoEventDeletionMode.SoftDelete),
                cancellation);
            return;
        }

        schedule.DeletedAt = DateTimeOffset.UtcNow;
        await eventScheduleRepository.UpdateAsync(schedule);
    }

    private async Task<EventSchedule> LoadScheduleAsync(long eventScheduleId)
    {
        return await eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(eventScheduleId)
            ?? throw new KeyNotFoundException($"EventSchedule {eventScheduleId} not found.");
    }

    private async Task EnsureNotSeasonPassOwnedAsync(long eventScheduleId)
    {
        if (await IsSeasonPassOwnedAsync(eventScheduleId))
        {
            throw new InvalidOperationException(
                $"EventSchedule {eventScheduleId} is owned by a SeasonPass bundle. " +
                "SeasonPass lifecycle owns Seats.io events created inside its season.");
        }
    }

    private async Task<bool> IsSeasonPassOwnedAsync(long eventScheduleId)
    {
        var links = await bundleEventScheduleRepository.GetByEventScheduleIdAsync(eventScheduleId);
        return links.Any(link => link.Bundle?.BundleType == BundleType.SeasonPass);
    }
}
