using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.API.Controllers
{
    // TODO: Move to Admin API and add summary documentation

    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly EventService _eventService;

        public EventsController(EventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        [EndpointName("GetEventsAsync")]
        public async Task<ActionResult<PagedResponse<EventListItem>>> GetEventsAsync(
            [FromQuery] string? venues = null,
            [FromQuery] string? categories = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "dateTime",
            [FromQuery] bool descending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var venueList = string.IsNullOrWhiteSpace(venues)
                ? null
                : venues.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            var categoryList = string.IsNullOrWhiteSpace(categories)
                ? null
                : categories
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => Enum.Parse<EventCategory>(c, ignoreCase: true))
                    .ToList();

            var result = await _eventService.GetEventListAsync(
                venueList,
                categoryList,
                startDate,
                endDate,
                search,
                sortBy,
                descending,
                page,
                pageSize);

            return Ok(result);
        }

        [HttpGet("{eventId}")]
        [EndpointName("GetEvenByIdAsync")]
        public async Task<ActionResult<EventListItem>> GetEventByIdAsync([FromRoute] long eventId)
        {
            var result = await _eventService.GetEventByIdAsync(eventId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
