using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBundleEventScheduleRepository
{
    Task InsertAsync(BundleEventSchedule entity);
    void Remove(BundleEventSchedule entity);
    Task CommitAsync();
    Task<List<BundleEventSchedule>> GetByBundleIdWithSchedulesAsync(long bundleId);
    Task<BundleEventSchedule?> GetByCompositeKeyAsync(long bundleId, long eventScheduleId);
    Task<bool> ExistsAsync(long bundleId, long eventScheduleId);
    Task<List<BundleEventSchedule>> GetByEventScheduleIdAsync(long eventScheduleId);
}
