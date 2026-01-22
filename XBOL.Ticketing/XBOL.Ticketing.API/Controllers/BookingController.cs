using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    /// <summary>
    /// Controller to handle booking operations.
    /// </summary>
    [Route("api/booking")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly SeatsIoService _seatsIoService;

        public BookingController(SeatsIoService seatsIoService)
        {
            _seatsIoService = seatsIoService;
        }

        /// <summary>
        /// Books the specified seat selection for the event and returns the identifiers of the booked seats.
        /// </summary>
        /// <param name="request">The booking request containing event and seat selection details. Cannot be null.</param>
        /// <returns>An action result containing a collection of strings that represent the keys of the successfully booked
        /// seats.</returns>
        [HttpPost("book-seats")]
        [EndpointName("BookSeatsAsync")]
        public async Task<ActionResult<IEnumerable<string>>> BookSeatsAsync([FromBody] BookingRequest request)
        {
            // TODO: Replace with event or order service Booking method
            ChangeObjectStatusResult result = await _seatsIoService.BookSeatsAsync(request);

            return Ok(result.Objects.Select(x => x.Key));
        }
    }
}
