namespace XBOL.Ticketing.Services.Event;

public interface IEventScheduleLifecycleService
{
    Task PublishAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default);

    Task SyncMetadataAsync(long eventScheduleId, CancellationToken cancellation = default);

    Task CancelAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default);

    Task DeleteAsync(long eventScheduleId, Guid userId, CancellationToken cancellation = default);
}
