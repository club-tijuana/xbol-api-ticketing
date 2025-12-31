using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueMapRepository(XBOLDbContext dbContext) : BaseRepository<VenueMap>(dbContext)
    {
    }
}
