using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Results;
using XBOL.Ticketing.Services.Event;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/event-schedules")]
    [ApiController]
    [Tags("Event Schedules")]
    public class EventScheduleController(EventScheduleService scheduleService) : ControllerBase
    {
        [HttpGet("{id:long}")]
        [EndpointName("GetEventScheduleById")]
        [ProducesResponseType(typeof(EventScheduleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventScheduleDTO>> GetScheduleByIdAsync(long id)
        {
            var result = await scheduleService.GetScheduleByIdAsync(id);
            if (result is null) { return NotFound(); }
            return Ok(result);
        }

        [HttpPost]
        [EndpointName("CreateEventSchedule")]
        [ProducesResponseType(typeof(EventScheduleResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<EventScheduleResponse>> CreateScheduleAsync(
            [FromBody] EventScheduleRequest request)
        {
            var result = await scheduleService.CreateEventScheduleAsync(request, GetUserId());
            return CreatedAtAction(nameof(GetScheduleByIdAsync), new { id = result.Id }, result);
        }

        [HttpPut("{id:long}")]
        [EndpointName("UpdateEventSchedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateScheduleAsync(
            long id,
            [FromBody] EventScheduleRequest request)
        {
            var result = await scheduleService.UpdateScheduleAsync(id, request, GetUserId());
            if (!result) { return NotFound(); }
            return NoContent();
        }

        [HttpPost("{id:long}/publish")]
        [EndpointName("PublishEventSchedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> PublishScheduleAsync(long id)
        {
            await scheduleService.PublishScheduleAsync(id, GetUserId());
            return NoContent();
        }

        [HttpPost("{id:long}/cancel")]
        [EndpointName("CancelEventSchedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CancelScheduleAsync(long id)
        {
            await scheduleService.CancelScheduleAsync(id, GetUserId());
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        [EndpointName("DeleteEventSchedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> DeleteScheduleAsync(long id)
        {
            var result = await scheduleService.DeleteScheduleAsync(id, GetUserId());
            if (!result) { return NotFound(); }
            return NoContent();
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return claim is not null ? Guid.Parse(claim) : Guid.Empty;
        }
    }
}
