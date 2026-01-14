using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Venue>(dbContext)
    {
        private readonly XBOLDbContext _context = dbContext;

        public async Task<List<string>> GetVenueNamesAsync()
        {
            return await _context.Venues
                .Select(v => v.Name)
                .ToListAsync();
        }
    }
}
