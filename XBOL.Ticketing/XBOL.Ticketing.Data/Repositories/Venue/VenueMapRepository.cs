using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueMapRepository(XBOLDbContext dbContext) : BaseRepository<VenueMap>(dbContext)
    {
        private readonly XBOLDbContext _context = dbContext;

        public async Task<List<VenueMapListItem>> GetVenueMapsAsync()
        {
            return await _context
                .VenueMaps.Select(vm => new VenueMapListItem
                {
                    Id = vm.Id,
                    VenueId = vm.VenueId,
                    Name = vm.Name,
                    ExternalMapKey = vm.ExternalMapKey,
                })
                .ToListAsync();
        }

        public async Task<VenueMapListItem?> GetVenueMapByIdAsync(long id)
        {
            return await _context
                .VenueMaps
                .Where(vm => vm.Id == id)
                .Select(vm => new VenueMapListItem
                {
                    Id = vm.Id,
                    VenueId = vm.VenueId,
                    Name = vm.Name,
                    ExternalMapKey = vm.ExternalMapKey,
                })
                .FirstOrDefaultAsync();
        }
    }
}
