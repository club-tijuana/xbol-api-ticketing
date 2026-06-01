using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventScheduleRepository(XBOLDbContext dbContext)
        : BaseRepository<EventSchedule>(dbContext), IEventScheduleRepository
    {
        public async Task<EventSchedule?> GetByIdWithEventAndVenueMapAsync(long id)
        {
            return await DbSet
                .Include(s => s.Event)
                .ThenInclude(e => e.VenueMap)
                .Include(s => s.Sections)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<EventSchedule?> GetByIdIncludingDeletedAsync(long id)
        {
            return await DbSet
                .IgnoreQueryFilters()
                .Include(s => s.Event)
                .ThenInclude(e => e.VenueMap)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
    }
}
