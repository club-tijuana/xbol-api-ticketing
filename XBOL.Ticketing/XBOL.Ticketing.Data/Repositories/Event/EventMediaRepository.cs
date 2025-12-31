using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventMediaRepository(XBOLDbContext dbContext) : BaseRepository<EventMedia>(dbContext)
    {
    }
}
