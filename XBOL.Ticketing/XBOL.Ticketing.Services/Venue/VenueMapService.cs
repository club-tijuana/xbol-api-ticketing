using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueMapService(VenueMapRepository repository) : BaseService<VenueMapRepository, VenueMap>(repository)
    {
    }
}
