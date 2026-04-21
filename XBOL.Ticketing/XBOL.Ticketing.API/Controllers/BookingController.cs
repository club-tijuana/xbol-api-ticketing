using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using SeatsioDotNet;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    /// <summary>
    /// Controller to handle booking operations.
    /// </summary>
    [Obsolete("Use ManageSeatsController hold and book endpoints instead.")]
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

            try
            {
                ChangeObjectStatusResult result = await _seatsIoService.BookEventSeatsAsync(request);
                return Ok(result.Objects.Select(x => x.Key));
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
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

            try
            {
                ChangeObjectStatusResult result = await _seatsIoService.BookSeasonSeatsAsync(request);
                return Ok(result.Objects.Select(x => x.Key));
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Releases the specified seats for an event or season, making them available for booking.
        /// </summary>
        /// <param name="request">The request containing the event or season key and the list of seat identifiers to release. The key cannot
        /// be null or empty, and the list of seats must contain at least one item.</param>
        /// <returns>An HTTP 200 response with the result of the release operation if seats were released;
        /// an HTTP 400 if the request is incomplete or an HTTP 404 response if the specified event or season does not exist.</returns>
        [HttpPost("release-seats")]
        [EndpointName("ReleaseBookedSeatsAsync")]
        [ProducesResponseType(typeof(ChangeObjectStatusResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ReleaseEventSeatsAsync([FromBody] ReleaseBookedSeatsRequest request)
        {
            if (string.IsNullOrEmpty(request.Key) || request.Seats.IsNullOrEmpty())
            {
                return BadRequest(ModelState);
            }

            if (await _seatsIoService.EventOrSeasonExistsAsync(request.Key) == false)
            {
                return NotFound("Event doesn't exists.");
            }

            if (await _seatsIoService.ValidateAllSeatsExistAsync(request.Key, request.Seats) == false)
            {
                return BadRequest("One or more seats doesn't exist.");
            }

            try
            {
                ChangeObjectStatusResult result = await _seatsIoService.ReleaseBookedSeatsAsync(request);
                return Ok(result);
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        private ObjectResult HandleSeatsioError(SeatsioException ex)
        {
            var response = new SeatsIoErrorResponse
            {
                RequestId = ex.RequestId,
                Errors = ex.Errors.Select(e => new SeatsIoErrorDetail
                {
                    Code = e.Code,
                    Message = e.Message
                }).ToList()
            };

            if (ex is RateLimitExceededException)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, response);
            }

            if (ex.Errors is { Count: > 0 })
            {
                var hasNotFound = ex.Errors.Any(e =>
                    e.Code.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase));

                if (hasNotFound)
                {
                    return StatusCode(StatusCodes.Status404NotFound, response);
                }

                return StatusCode(StatusCodes.Status400BadRequest, response);
            }

            return StatusCode(StatusCodes.Status502BadGateway, response);
        }
    }
}
