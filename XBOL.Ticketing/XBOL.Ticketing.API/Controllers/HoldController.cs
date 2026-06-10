using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.HoldTokens;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Season;

namespace XBOL.Ticketing.API.Controllers
{
    [Obsolete("Use ManageSeatsController hold endpoints instead.")]
    [Route("api/hold-seats")]
    [ApiController]
    public class HoldController(SeatsIoService seatsIoService, EventService eventService, SeasonService seasonService, IBundleRepository bundleRepository) : ControllerBase
    {
        /// <summary>
        /// Asynchronously holds the specified seats for an event and returns a token representing the hold.
        /// </summary>
        /// <remarks>This method reserves the requested seats for a limited time, allowing further actions
        /// such as purchase or release. If the event is not found or the seat information is invalid, the operation may
        /// fail. Exception handling should be implemented to manage such cases appropriately.</remarks>
        /// <param name="request">The request containing the event identifier and the collection of seat identifiers to be held. The event
        /// identifier must correspond to an existing event, and the seat identifiers must be valid for that event.</param>
        /// <returns>An ActionResult containing a HoldToken that represents the hold placed on the specified seats.</returns>
        [HttpPost]
        [EndpointName("HoldSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken>> HoldSeatsAsync(HoldSeatsRequest request)
        {
            // TODO: Handle exceptions, not found, and errors

            string? eventKey;
            switch (request.SaleType)
            {
                case SaleType.SeasonPass:
                    {
                        var season = await seasonService.GetByIdAsync(request.EventScheduleId);
                        eventKey = season?.ExternalSeasonKey ?? string.Empty;
                        break;
                    }

                case SaleType.Bundle:
                    {
                        var bundle = await bundleRepository.GetByIdAsync(request.EventScheduleId);
                        eventKey = bundle?.ExternalKey ?? string.Empty;
                        break;
                    }

                case SaleType.Event:
                    {
                        eventKey = await eventService.GetEventKeyAsync(request.EventScheduleId) ?? string.Empty;
                        break;
                    }
                default:
                    eventKey = string.Empty;
                    break;
            }

            var token = await seatsIoService.CreateHoldTokenAsync(eventKey);

            await seatsIoService.HoldSeatsAsync(eventKey, request.Seats.ToArray(), token.Token);

            return Ok(token);
        }

        [HttpPost("client")]
        [EndpointName("ClientHoldSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken>> ClientHoldSeatsAsync()
        {
            var token = await seatsIoService.CreateHoldTokenAsync();

            return Ok(token);
        }

        /// <summary>
        /// Asynchronously holds the specified seats for a season and returns a token representing the hold.
        /// </summary>
        /// <remarks>This method reserves the requested seats for a limited time, allowing further actions
        /// such as purchase or release. If the event is not found or the seat information is invalid, the operation may
        /// fail. Exception handling should be implemented to manage such cases appropriately.</remarks>
        /// <param name="request">The request containing the event identifier and the collection of seat identifiers to be held. The season
        /// identifier must correspond to an existing season, and the seat identifiers must be valid for that season.</param>
        /// <returns>An ActionResult containing a HoldToken that represents the hold placed on the specified seats.</returns>
        [HttpPost("season")]
        [EndpointName("HoldSeasonSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken>> GetSeasonKeyAsync(HoldSeasonSeatsRequest request)
        {
            // TODO: Handle exceptions, not found, and errors
            var seasonKey = await eventService.GetSeasonKeyAsync(request.SeasonId) ?? string.Empty;

            var token = await seatsIoService.CreateHoldTokenAsync(seasonKey); // TODO: Get this value from a setting or config

            await seatsIoService.HoldSeatsAsync(seasonKey, request.Seats.ToArray(), token.Token);

            return Ok(token);
        }

        /// <summary>
        /// Retrieves the hold token associated with the specified token string.
        /// </summary>
        /// <remarks>This method is asynchronous and may involve network calls to retrieve the hold token.
        /// Ensure that the hold token is valid before calling this method.</remarks>
        /// <param name="holdToken">The hold token string used to identify the specific hold request. This parameter cannot be null or empty.</param>
        /// <returns>An ActionResult containing the HoldToken object that matches the provided hold token. Returns an error
        /// response if the token is invalid or not found.</returns>
        [HttpGet]
        [EndpointName("GetHoldTokenAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken>> GetHoldTokenAsync([FromQuery] string holdToken)
        {
            var result = await seatsIoService.GetHoldTokenAsync(holdToken);
            return Ok(result);
        }

        /// <summary>
        /// Releases the hold on seats associated with the specified hold token.
        /// </summary>
        /// <remarks>This method is asynchronous and should be awaited. It communicates with the seat
        /// management service to release the hold, and may throw exceptions if the hold token is invalid or if service
        /// errors occur.</remarks>
        /// <param name="holdToken">The token that identifies the hold to be released. This value must be a valid, non-null hold token.</param>
        /// <returns>An ActionResult containing the released HoldToken if the operation is successful; otherwise, a not found
        /// result.</returns>
        [HttpDelete]
        [EndpointName("ReleaseHoldSeatsAsync")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HoldToken))]
        public async Task<ActionResult<HoldToken?>> ReleaseHoldSeatsAsync([FromQuery] string holdToken)
        {
            var result = await seatsIoService.ReleaseHoldTokenAsync(holdToken);
            return Ok(result);
        }
    }
}
