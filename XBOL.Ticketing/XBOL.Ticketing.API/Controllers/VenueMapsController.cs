using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Venue;

namespace XBOL.Ticketing.API.Controllers
{
    // TODO: Move to Admin API

    [ApiController]
    [Route("api/venue-maps")]
    public class VenueMapsController : ControllerBase
    {
        [HttpGet]
        [EndpointName("GetVenueMaps")]
        public async Task<ActionResult<List<VenueMapListItem>>> GetVenueMaps(
            [FromServices] VenueMapService service
        )
        {
            var venueMaps = await service.GetVenueMapListAsync();
            return Ok(venueMaps);
        }

        [HttpGet("{id:long}")]
        [EndpointName("GetVenueMap")]
        public async Task<ActionResult<VenueMapListItem>> GetVenueMap(
            [FromServices] VenueMapService service,
            [FromRoute] long id
        )
        {
            var result = await service.GetVenueMapByIdAsync(id);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
