using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;
using VenueModel = XBOL.Ticketing.Core.Model.Venue;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueService(VenueRepository repository)
        : BaseService<VenueRepository, VenueModel>(repository)
    {
        public async Task<List<VenueListItem>> GetVenuesAsync()
        {
            return await Repository.GetVenuesAsync();
        }
    }
}
