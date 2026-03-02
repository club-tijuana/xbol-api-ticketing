using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.DTO.Requests;
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
        /// Books event seats based on the specified booking request and returns the keys of the successfully booked
        /// seats.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to book seats for an event. If the
        /// booking request is invalid or if the booking process fails due to service issues, an appropriate error
        /// response is returned.</remarks>
        /// <param name="request">The booking request containing details of the event seats to be booked. This parameter must not be null and
        /// should include valid seat identifiers.</param>
        /// <returns>An action result containing a collection of strings that represent the keys of the booked event seats.</returns>
        [HttpPost("event/book-seats")]
        [EndpointName("BookEventSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        public async Task<ActionResult<IEnumerable<string>>> BookEventSeatsAsync([FromBody] EventBookingRequest request)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            ChangeObjectStatusResult result = await _seatsIoService.BookEventSeatsAsync(request);

            return Ok(result.Objects.Select(x => x.Key));
        }

        /// <summary>
        /// Books season seats based on the specified booking request and returns the keys of the successfully booked
        /// seats.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to book seats for a season. If the
        /// booking request is invalid or if the booking process fails due to service issues, an appropriate error
        /// response is returned.</remarks>
        /// <param name="request">The booking request containing details of the season seats to be booked. This parameter must not be null and
        /// should include valid seat identifiers.</param>
        /// <returns>An action result containing a collection of strings that represent the keys of the booked season seats.</returns>
        [HttpPost("season/book-seats")]
        [EndpointName("BookSeasonSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        public async Task<ActionResult<IEnumerable<string>>> BookSeasonSeatsAsync([FromBody] SeasonBookingRequest request)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            ChangeObjectStatusResult result = await _seatsIoService.BookSeasonSeatsAsync(request);

            return Ok(result.Objects.Select(x => x.Key));
        }
    }
}
