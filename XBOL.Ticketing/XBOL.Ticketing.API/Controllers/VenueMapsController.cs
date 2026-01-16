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
        public async Task<ActionResult<List<VenueMapListItem>>> GetVenueMaps(
            [FromServices] VenueMapService service
        )
        {
            var venueMaps = await service.GetVenueMapListAsync();
            return Ok(venueMaps);
        }
    }
}
