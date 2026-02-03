using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Base;
using VenueModel = XBOL.Ticketing.Core.Model.Venue;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueRepository(XBOLDbContext dbContext) : BaseRepository<VenueModel>(dbContext)
    {
        private readonly XBOLDbContext _context = dbContext;

        public async Task<List<VenueListItem>> GetVenuesAsync()
        {
            return await _context
                .Venues.Select(vm => new VenueListItem { Id = vm.Id, Name = vm.Name })
                .ToListAsync();
        }
    }
}
