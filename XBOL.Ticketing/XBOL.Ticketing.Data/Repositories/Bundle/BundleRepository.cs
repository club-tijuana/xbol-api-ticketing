using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Bundle
{
    public class BundleRepository(XBOLDbContext dbContext)
        : BaseRepository<Core.Model.Bundle>(dbContext), IBundleRepository
    {
        public async Task<Core.Model.Bundle?> GetByIdWithVenueMapAndSchedulesAsync(long id)
        {
            return await dbContext.Bundles
                .Include(bundle => bundle.VenueMap)
                .Include(bundle => bundle.BundleSections)
                .ThenInclude(section => section.BundleSeats)
                .Include(bundle => bundle.BundleEventSchedules)
                .ThenInclude(link => link.EventSchedule)
                .ThenInclude(schedule => schedule.Sections)
                .FirstOrDefaultAsync(bundle => bundle.Id == id);
        }

        public async Task<List<EventCategory>> GetCategoriesByIdsAsync(IReadOnlyCollection<long> categoryIds)
        {
            return categoryIds.Count == 0
                ? []
                : await dbContext.EventCategories
                    .Where(category => categoryIds.Contains(category.Id))
                    .ToListAsync();
        }
    }
}
