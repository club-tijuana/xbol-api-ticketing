using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Venue;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("api/venues")]
    public class VenuesController : ControllerBase
    {
        [HttpGet]
        [EndpointName("GetVenues")]
        public async Task<ActionResult<List<VenueListItem>>> GetVenues(
            [FromServices] VenueService service
        )
        {
            var venues = await service.GetVenueListAsync();
            return Ok(venues);
        }
    }
}
