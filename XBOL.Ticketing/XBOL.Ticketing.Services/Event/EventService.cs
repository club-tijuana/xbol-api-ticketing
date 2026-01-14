using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventService(EventRepository repository, SeatsIoService seatsIoService)
        : BaseService<EventRepository, Core.Model.Event>(repository)
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

        public async Task<PagedResponse<EventListItem>> GetEventListAsync(
            List<string>? venues = null,
            List<EventCategory>? categories = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            string? search = null,
            string sortBy = "dateTime",
            bool descending = false,
            int page = 1,
            int pageSize = 20
        )
        {
            var (items, totalCount) = await Repository.GetEventListAsync(
                venues,
                categories,
                startDate,
                endDate,
                search,
                sortBy,
                descending,
                page,
                pageSize
            );

            return new PagedResponse<EventListItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}
