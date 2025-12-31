using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventSectionRepository(XBOLDbContext dbContext) : BaseRepository<EventSection>(dbContext)
    {
    }
}
