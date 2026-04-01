using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet;
using SeatsioDotNet.EventReports;
using SeatsioDotNet.HoldTokens;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/manage-seats")]
    [ApiController]
    public class ManageSeatsController(SeatsIoService seatsIoService) : ControllerBase
    {
        /// <summary>
        /// Retrieves the current status, forSale flag, and extraData for the specified seats.
        /// </summary>
        /// <param name="eventKey">The Seats.io event key.</param>
        /// <param name="seatKeys">One or more seat object labels to query.</param>
        /// <returns>A dictionary of seat keys to their object info.</returns>
        [HttpGet]
        [EndpointName("GetSeatsInfoAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, EventObjectInfo>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> GetSeatsInfoAsync(
            [FromQuery] string eventKey,
            [FromQuery] string[] seatKeys)
        {
            try
            {
                var result = await seatsIoService.GetSeatsInfoAsync(eventKey, seatKeys);
                return Ok(result);
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Sets the for-sale configuration for the specified seats in a Seats.io event.
        /// </summary>
        /// <param name="request">The event key, seat keys, and whether the seats should be marked as for sale or not for sale.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("for-sale")]
        [EndpointName("SetForSaleAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> SetForSaleAsync([FromBody] SetForSaleRequest request)
        {
            try
            {
                await seatsIoService.SetForSaleAsync(request.EventKey, request.SeatKeys, request.ForSale);
                return NoContent();
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Updates the extra data (reason, color) for the specified seats in a Seats.io event.
        /// Send an empty object to clear extra data.
        /// </summary>
        /// <param name="request">The event key, seat keys, and extra data to set on each seat.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("extra-data")]
        [EndpointName("UpdateSeatExtraDataAsync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> UpdateExtraDataAsync([FromBody] UpdateSeatExtraDataRequest request)
        {
            try
            {
                await seatsIoService.UpdateExtraDataAsync(request.EventKey, request.SeatKeys, request.ExtraData);
                return NoContent();
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Releases the specified seats, resetting their status to free.
        /// </summary>
        /// <param name="request">The event key and seat labels to release.</param>
        /// <returns>The keys of the released seats.</returns>
        [HttpPost("release")]
        [EndpointName("ReleaseSeatsActionAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> ReleaseSeatsActionAsync([FromBody] ReleaseSeatsByKeyRequest request)
        {
            try
            {
                var result = await seatsIoService.ReleaseSeatsAsync(
                    request.EventKey,
                    request.Seats.ToArray(),
                    request.HoldToken,
                    request.KeepExtraData,
                    request.IgnoreChannels,
                    request.ChannelKeys?.ToArray());
                return Ok(result.Objects.Select(x => x.Key));
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Retrieves the hold token details for the specified token string.
        /// </summary>
        /// <param name="holdToken">The hold token string to look up.</param>
        /// <returns>The hold token with expiration details.</returns>
        [HttpGet("hold/{holdToken}")]
        [EndpointName("GetHoldTokenActionAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> GetHoldTokenActionAsync([FromRoute] string holdToken)
        {
            try
            {
                var result = await seatsIoService.GetHoldTokenAsync(holdToken);
                return Ok(result);
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Expires the specified hold token, releasing all seats held by it.
        /// </summary>
        /// <param name="holdToken">The hold token to expire.</param>
        /// <returns>The expired hold token, or not found.</returns>
        [HttpDelete("hold/{holdToken}")]
        [EndpointName("ExpireHoldTokenActionAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> ExpireHoldTokenActionAsync([FromRoute] string holdToken)
        {
            try
            {
                var result = await seatsIoService.ReleaseHoldTokenAsync(holdToken);
                return Ok(result);
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Holds the specified seats by creating a temporary hold token.
        /// </summary>
        /// <param name="request">The event key and seat labels to hold.</param>
        /// <returns>A hold token with expiration details.</returns>
        [HttpPost("hold")]
        [EndpointName("HoldSeatsActionAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> HoldSeatsActionAsync([FromBody] HoldSeatsActionRequest request)
        {
            try
            {
                var token = await seatsIoService.CreateHoldTokenAsync(5); // TODO: Get this value from a setting or config
                await seatsIoService.HoldSeatsAsync(request.EventKey, request.Seats.ToArray(), token.Token);
                return Ok(token);
            }
            catch (SeatsioException ex)
            {
                return HandleSeatsioError(ex);
            }
        }

        /// <summary>
        /// Books the specified seats, optionally consuming a hold token.
        /// </summary>
        /// <param name="request">The event key, seats with prices, hold token, and booking details.</param>
        /// <returns>The keys of the successfully booked seats.</returns>
        [HttpPost("book")]
        [EndpointName("BookSeatsActionAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(SeatsIoErrorResponse))]
        [ProducesResponseType(StatusCodes.Status502BadGateway, Type = typeof(SeatsIoErrorResponse))]
        public async Task<IActionResult> BookSeatsActionAsync([FromBody] BookSeatsActionRequest request)
        {
            try
            {
                var result = await seatsIoService.BookSeatsAsync(request.EventKey, request.Seats, request.HoldToken);
                return Ok(result.Objects.Select(x => x.Key));
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
