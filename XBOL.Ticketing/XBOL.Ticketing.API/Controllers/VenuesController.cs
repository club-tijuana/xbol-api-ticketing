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

        /// <summary>
        /// Retrieves a list of available venues asynchronously.
        /// </summary>
        /// <remarks>This method is intended for scenarios where venue data is required. It performs a
        /// non-blocking operation by leveraging the underlying venue service to fetch venue information
        /// asynchronously.</remarks>
        /// <returns>An object containing a list of objects that represent the
        /// available venues. The list will be empty if no venues are found.</returns>
        [HttpGet]
        [EndpointName("GetVenuesAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VenueListItem>))]
        public async Task<ActionResult<List<VenueListItem>>> GetVenuesAsync()
        {
            var venues = await _venueService.GetVenuesAsync();
            return Ok(venues);
        }
    }
}
