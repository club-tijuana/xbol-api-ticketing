using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/event-catalog")]
    [ApiController]
    [Tags("Event Catalog")]
    public class EventCatalogController(EventCatalogService eventCatalogService) : ControllerBase
    {
        [HttpGet]
        [EndpointName("GetEventCatalogItems")]
        [ProducesResponseType(typeof(PagedResponse<EventCatalogItemDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<EventCatalogItemDTO>>> GetEventCatalogItems(
            [FromQuery] EventCatalogQueryParams queryParams)
        {
            var result = await eventCatalogService.GetItemsAsync(queryParams);
            return Ok(result);
        }

        [HttpGet("bundles/{bundleId:long}/schedules")]
        [EndpointName("GetBundleScheduleItems")]
        [ProducesResponseType(typeof(PagedResponse<BundleScheduleItemDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedResponse<BundleScheduleItemDTO>>> GetBundleScheduleItems(
            [FromRoute] long bundleId,
            [FromQuery] BundleScheduleQueryParams queryParams)
        {
            try
            {
                var result = await eventCatalogService.GetBundleScheduleItemsAsync(bundleId, queryParams);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
