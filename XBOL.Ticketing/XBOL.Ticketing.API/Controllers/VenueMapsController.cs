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
        private readonly VenueMapService _venueMapService;

        public VenueMapsController(VenueMapService venueMapService)
        {
            _venueMapService = venueMapService;
        }

        [HttpGet]
        public async Task<ActionResult<List<VenueMapListItem>>> GetVenueMapsAsync()
        {
            var venueMaps = await _venueMapService.GetVenueMapListAsync();
            return Ok(venueMaps);
        }

        [HttpGet("{venueMapId}")]
        public async Task<ActionResult<VenueMapListItem>> GetVenueMapByIdAsync([FromRoute] long venueMapId)
        {
            var result = await _venueMapService.GetVenueMapByIdAsync(venueMapId);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
    }
}
