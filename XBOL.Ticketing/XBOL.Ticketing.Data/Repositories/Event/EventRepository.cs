using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventRepository(XBOLDbContext dbContext) : BaseRepository<Core.Model.Event>(dbContext)
    {
    }
}
