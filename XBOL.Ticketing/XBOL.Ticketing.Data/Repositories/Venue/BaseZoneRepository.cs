using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Venue
{
    public class BaseZoneRepository(XBOLDbContext dbContext) : BaseRepository<BaseZone>(dbContext)
    {
    }
}
