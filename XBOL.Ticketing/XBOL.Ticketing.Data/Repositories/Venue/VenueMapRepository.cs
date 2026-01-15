using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueMapRepository(XBOLDbContext dbContext) : BaseRepository<VenueMap>(dbContext)
    {
        private readonly XBOLDbContext _context = dbContext;

        public async Task<List<VenueMapListItem>> GetVenueMapListAsync()
        {
            return await _context
                .VenueMaps.Select(vm => new VenueMapListItem
                {
                    Id = vm.Id,
                    Name = vm.Name,
                    ExternalMapKey = vm.ExternalMapKey,
                })
                .ToListAsync();
        }
    }
}
