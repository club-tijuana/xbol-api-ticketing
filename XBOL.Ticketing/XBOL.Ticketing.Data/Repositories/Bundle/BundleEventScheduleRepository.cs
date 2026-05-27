using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleEventScheduleRepository(XBOLDbContext dbContext)
        : IBundleEventScheduleRepository
    {
        private readonly DbSet<BundleEventSchedule> _dbSet = dbContext.Set<BundleEventSchedule>();

        public Task CommitAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(long bundleId, long eventScheduleId)
        {
            throw new NotImplementedException();
        }

        public Task<List<BundleEventSchedule>> GetByBundleIdWithSchedulesAsync(long bundleId)
        {
            throw new NotImplementedException();
        }

        public Task<BundleEventSchedule?> GetByCompositeKeyAsync(long bundleId, long eventScheduleId)
        {
            throw new NotImplementedException();
        }

        public Task<List<BundleEventSchedule>> GetByEventScheduleIdAsync(long eventScheduleId)
        {
            throw new NotImplementedException();
        }

        public async Task InsertAsync(BundleEventSchedule entity) => await _dbSet.AddAsync(entity);
        public void Remove(BundleEventSchedule entity) => _dbSet.Remove(entity);
    }
}
