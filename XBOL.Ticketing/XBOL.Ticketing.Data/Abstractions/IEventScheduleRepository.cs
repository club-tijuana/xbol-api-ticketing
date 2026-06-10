using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IEventScheduleRepository
{
    Task<EventSchedule?> GetByIdAsync(long id);
    Task<EventSchedule?> GetByIdWithEventAndVenueMapAsync(long id);
    Task<EventSchedule?> GetByIdIncludingDeletedAsync(long id);
    Task UpdateAsync(EventSchedule entity);
}
