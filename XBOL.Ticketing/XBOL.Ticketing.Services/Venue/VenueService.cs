using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueService(VenueRepository repository) : BaseService<VenueRepository, Core.Model.Venue>(repository)
    {
        public async Task<List<string>> GetVenueNamesAsync()
        {
            return await Repository.GetVenueNamesAsync();
        }
    }
}
