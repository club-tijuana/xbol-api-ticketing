using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IEventScheduleRepository
{
    Task<EventSchedule?> GetByIdAsync(long id);
}
