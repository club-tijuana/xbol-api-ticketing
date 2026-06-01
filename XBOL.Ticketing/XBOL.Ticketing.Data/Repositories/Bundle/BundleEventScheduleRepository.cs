using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleEventScheduleRepository(XBOLDbContext dbContext)
        : IBundleEventScheduleRepository
    {
        private readonly DbSet<BundleEventSchedule> _dbSet = dbContext.Set<BundleEventSchedule>();

        public async Task InsertAsync(BundleEventSchedule entity) => await _dbSet.AddAsync(entity);
        public void Remove(BundleEventSchedule entity) => _dbSet.Remove(entity);

        public async Task<List<BundleEventSchedule>> GetByBundleIdWithSchedulesAsync(long bundleId)
        {
            return await _dbSet
                .Include(bes => bes.EventSchedule)
                .Where(bes => bes.BundleId == bundleId)
                .OrderBy(bes => bes.SortOrder)
                .ToListAsync();
        }

        public async Task<BundleEventSchedule?> GetByCompositeKeyAsync(long bundleId, long eventScheduleId)
        {
            return await _dbSet.FindAsync(bundleId, eventScheduleId);
        }

        public async Task<bool> ExistsAsync(long bundleId, long eventScheduleId)
        {
            return await _dbSet.AnyAsync(bes => bes.BundleId == bundleId && bes.EventScheduleId == eventScheduleId);
        }

        public async Task<List<BundleEventSchedule>> GetByEventScheduleIdAsync(long eventScheduleId)
        {
            return await _dbSet
                .Include(bes => bes.Bundle)
                .Where(bes => bes.EventScheduleId == eventScheduleId)
                .ToListAsync();
        }

        public async Task CommitAsync() => await dbContext.SaveChangesAsync();
    }
}
