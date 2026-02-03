using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueMapService(VenueMapRepository repository) : BaseService<VenueMapRepository, VenueMap>(repository)
    {
        public async Task<List<VenueMapListItem>> GetVenueMapsAsync()
        {
            return await Repository.GetVenueMapsAsync();
        }

        public async Task<VenueMapListItem?> GetVenueMapByIdAsync(long id)
        {
            return await Repository.GetVenueMapByIdAsync(id);
        }
    }
}
