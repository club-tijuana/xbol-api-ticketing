using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventService(EventRepository repository, SeatsIoService seatsIoService) : BaseService<EventRepository, Core.Model.Event>(repository)
    {
        public async Task BookSeatsAsync(BookingRequest request)
        {
            ChangeObjectStatusResult result = await seatsIoService.BookSeatsAsync(request);

            // Get user
            foreach (var item in result.Objects)
            {
                // Save Ticket
            }

            // Get Total

            // Process Payment

        }
    }
}
