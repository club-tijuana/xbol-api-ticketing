using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventService(EventRepository repository) : BaseService<EventRepository, Core.Model.Event>(repository)
    {
    }
}
