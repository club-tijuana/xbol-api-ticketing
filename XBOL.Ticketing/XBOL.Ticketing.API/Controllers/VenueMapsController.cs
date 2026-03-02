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

        /// <summary>
        /// Retrieves a collection of venue maps available in the system.
        /// </summary>
        /// <remarks>This method is intended for use in a web API context and asynchronously calls the
        /// underlying venue map service to obtain the data. The response will contain an empty list if no venue maps
        /// are available.</remarks>
        /// <returns>An HTTP response containing a list of objects that represent the available
        /// venue maps.</returns>
        [HttpGet]
        [EndpointName("GetVenueMapsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VenueMapListItem>))]
        public async Task<ActionResult<List<VenueMapListItem>>> GetVenueMapsAsync()
        {
            var venueMaps = await _venueMapService.GetVenueMapsAsync();
            return Ok(venueMaps);
        }

        /// <summary>
        /// Retrieves the venue map associated with the specified identifier.
        /// </summary>
        /// <remarks>This method asynchronously fetches the venue map from the service. If no venue map is
        /// found for the given identifier, a 404 Not Found response is returned.</remarks>
        /// <param name="venueMapId">The unique identifier of the venue map to retrieve. Must be a positive long value.</param>
        /// <returns>An ActionResult containing a VenueMapListItem if found; otherwise, returns a NotFound result.</returns>
        [HttpGet("{venueMapId}")]
        [EndpointName("GetVenueMapByIdAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VenueMapListItem))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
