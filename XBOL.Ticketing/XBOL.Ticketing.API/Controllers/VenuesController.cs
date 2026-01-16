using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Venue;

namespace XBOL.Ticketing.API.Controllers
{
    // TODO: Move to Admin API

    [ApiController]
    [Route("api/venues")]
    public class VenuesController : ControllerBase
    {
        private readonly VenueService _venueService;

        public VenuesController(VenueService venueService)
        {
            _venueService = venueService;
        }

        [HttpGet]
        public async Task<ActionResult<List<VenueListItem>>> GetVenuesAsync()
        {
            var venues = await _venueService.GetVenuesAsync();
            return Ok(venues);
        }
    }
}
