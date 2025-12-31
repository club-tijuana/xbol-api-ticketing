using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Event;

using XBOL.Ticketing.Services.Base;
namespace XBOL.Ticketing.Services.Event
{
    public class EventScheduleService(EventScheduleRepository repository) : BaseService<EventScheduleRepository, EventSchedule>(repository)
    {
    }
}
