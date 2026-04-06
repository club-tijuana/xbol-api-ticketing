using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Responses;
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

        /// <summary>
        /// Retrieves a paginated list of events filtered by venue, category, date range, and search criteria.
        /// </summary>
        /// <param name="venues">A comma-separated list of venue identifiers used to filter the events. If null or empty, events from all
        /// venues are included.</param>
        /// <param name="categories">A comma-separated list of event category names used to filter the events. If null or empty, events from all
        /// categories are included.</param>
        /// <param name="startDate">The start date and time for filtering events. Only events occurring on or after this date are included. If
        /// null, no lower date bound is applied.</param>
        /// <param name="endDate">The end date and time for filtering events. Only events occurring on or before this date are included. If
        /// null, no upper date bound is applied.</param>
        /// <param name="search">A search term used to filter events by name or description. If null or empty, no search filtering is
        /// applied.</param>
        /// <param name="sortBy">The field by which to sort the events. Defaults to "dateTime" if not specified.</param>
        /// <param name="descending">Indicates whether the results should be sorted in descending order. Set to <see langword="true"/> to sort
        /// descending; otherwise, ascending.</param>
        /// <param name="page">The page number of results to retrieve. Must be greater than zero.</param>
        /// <param name="pageSize">The maximum number of events to include in a single page of results. Must be greater than zero.</param>
        /// <returns>An object containing the filtered and paginated list of events.</returns>
        [HttpGet]
        [EndpointName("GetEventsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResponse<EventListItem>))]
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

        /// <summary>
        /// Retrieves the details of the event specified by its unique identifier.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to fetch event details from the event
        /// service. If the event is not found, a 404 Not Found response is returned.</remarks>
        /// <param name="eventId">The unique identifier of the event to retrieve. Must be a positive long value.</param>
        /// <returns>An ActionResult containing the event details if found; otherwise, a NotFound result if no event exists for
        /// the specified identifier.</returns>
        [HttpGet("{eventId}")]
        [EndpointName("GetEvenByIdAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EventListItem))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
