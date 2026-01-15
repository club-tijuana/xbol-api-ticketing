using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueService(VenueRepository repository)
        : BaseService<VenueRepository, Core.Model.Venue>(repository) { }
}
