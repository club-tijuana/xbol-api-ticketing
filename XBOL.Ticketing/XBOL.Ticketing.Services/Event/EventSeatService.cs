using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventSeatService(EventSeatRepository repository) : BaseService<EventSeatRepository, EventSeat>(repository)
    {
        public async Task UpdatePricesFromList(List<(long, decimal)> eventSeats) => await repository.UpdatePricesFromList(eventSeats);
    }
}