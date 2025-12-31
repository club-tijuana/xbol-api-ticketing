using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class VenueRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Venue>(dbContext)
    {
    }
}
