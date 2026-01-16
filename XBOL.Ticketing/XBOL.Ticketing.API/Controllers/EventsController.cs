using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.API.Controllers
{
    // TODO: Move to Admin API

    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        [HttpGet]
        [EndpointName("GetEvents")]
        public async Task<ActionResult<PagedResponse<EventListItem>>> GetEvents(
            [FromServices] EventService service,
            [FromQuery] string? venues = null,
            [FromQuery] string? categories = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "dateTime",
            [FromQuery] bool descending = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
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

            var result = await service.GetEventListAsync(
                venueList,
                categoryList,
                startDate,
                endDate,
                search,
                sortBy,
                descending,
                page,
                pageSize
            );

            return Ok(result);
        }

        [HttpGet("{id:long}")]
        [EndpointName("GetEvent")]
        public async Task<ActionResult<EventListItem>> GetEvent(
            [FromServices] EventService service,
            [FromRoute] long id
        )
        {
            var result = await service.GetEventByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
