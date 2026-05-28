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
                .Include(bundle => bundle.BundleEventSchedules)
                .ThenInclude(link => link.EventSchedule)
                .FirstOrDefaultAsync(bundle => bundle.Id == id);
        }
    }
}
