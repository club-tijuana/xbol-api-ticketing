using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.HoldTokens;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/hold-seats")]
    [ApiController]
    public class HoldController(SeatsIoService seatsIoService, EventService eventService) : ControllerBase
    {
        [HttpPost]
        [EndpointName("HoldSeatsAsync")]
        public async Task<ActionResult<HoldToken>> HoldSeatsAsync(HoldSeatsRequest request)
        {
            // TODO: Handle exceptions, not found, and errors
            var eventKey = await eventService.GetEventKeyAsync(request.EventId) ?? string.Empty;

            var token = await seatsIoService.CreateHoldTokenAsync(1);

            await seatsIoService.HoldSeatsAsync(eventKey, request.Seats.ToArray(), token.Token);

            return Ok(token);
        }

        [HttpGet]
        [EndpointName("GetHoldTokenAsync")]
        public async Task<ActionResult<HoldToken>> GetHoldTokenAsync([FromQuery] string holdToken)
        {
            var result = await seatsIoService.GetHoldTokenAsync(holdToken);
            return Ok(result);
        }

        [HttpDelete]
        [EndpointName("ReleaseHoldSeatsAsync")]
        public async Task<ActionResult<HoldToken>> ReleaseHoldSeatsAsync([FromQuery] string holdToken)
        {
            var result = await seatsIoService.ReleaseHoldTokenAsync(holdToken);
            return Ok(result);
        }
    }
}
