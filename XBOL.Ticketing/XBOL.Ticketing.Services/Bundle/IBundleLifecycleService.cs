namespace XBOL.Ticketing.Services.Bundle;

public interface IBundleLifecycleService
{
    Task PublishAsync(long bundleId, Guid userId, CancellationToken cancellation = default);

    Task CancelAsync(long bundleId, Guid userId, CancellationToken cancellation = default);

    Task SyncMetadataAsync(long bundleId, CancellationToken cancellation = default);

    Task AddSchedulesAsync(
        long bundleId,
        IReadOnlyCollection<long> eventScheduleIds,
        CancellationToken cancellation = default);
}
