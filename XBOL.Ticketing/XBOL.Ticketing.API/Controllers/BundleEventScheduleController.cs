using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.API.Controllers
{
    /// <summary>
    /// Manage EventSchedule associations for a Bundle.
    /// </summary>
    /// <remarks>
    /// Bundles are created via the Bundles endpoint. This controller handles
    /// adding, listing, and removing EventSchedules from an existing Bundle.
    /// Modifications are allowed when the Bundle is in Draft, PendingReview, or Approved status.
    /// Published Basic bundles may update membership locally. Published SeasonPass bundles may receive
    /// new EventSchedules created inside the season, but existing SeasonPass membership cannot be removed.
    /// </remarks>
    [Route("api/bundles/{bundleId:long}/schedules")]
    [ApiController]
    [Tags("Bundle Event Schedules")]
    public class BundleEventScheduleController(BundleEventScheduleService bundleEventScheduleService) : ControllerBase
    {
        /// <summary>
        /// List all EventSchedules in a Bundle.
        /// </summary>
        [HttpGet]
        [EndpointName("GetBundleEventSchedules")]
        [ProducesResponseType(typeof(List<BundleEventScheduleResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<BundleEventScheduleResponseDTO>>> GetBundleEventSchedules(
            long bundleId)
        {
            var result = await bundleEventScheduleService.GetByBundleAsync(bundleId);
            return Ok(result);
        }

        /// <summary>
        /// Add EventSchedules to a Bundle.
        /// </summary>
        /// <remarks>
        /// Returns the updated list of all EventSchedules in the Bundle.
        ///
        /// SeasonPass validation rules:
        /// - Cannot add an EventSchedule that already has an ExternalEventKey (already synced to Seats.io as standalone).
        /// - An EventSchedule can belong to at most one SeasonPass bundle (Seats.io 1-season-parent constraint).
        /// </remarks>
        [HttpPost]
        [EndpointName("AddBundleEventSchedules")]
        [ProducesResponseType(typeof(List<BundleEventScheduleResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<List<BundleEventScheduleResponseDTO>>> AddBundleEventSchedules(
            long bundleId, [FromBody] BundleEventScheduleAddRequest request)
        {
            var result = await bundleEventScheduleService.AddAsync(bundleId, request);
            return Ok(result);
        }

        /// <summary>
        /// Remove a single EventSchedule from a Bundle.
        /// </summary>
        [HttpDelete("{eventScheduleId:long}")]
        [EndpointName("RemoveBundleEventSchedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> RemoveBundleEventSchedule(
            long bundleId, long eventScheduleId)
        {
            await bundleEventScheduleService.RemoveAsync(bundleId, eventScheduleId);
            return NoContent();
        }

        /// <summary>
        /// Remove multiple EventSchedules from a Bundle.
        /// </summary>
        /// <remarks>
        /// EventSchedules not currently associated with the Bundle are silently skipped.
        /// Returns the count of associations actually removed.
        /// </remarks>
        [HttpDelete]
        [EndpointName("RemoveBundleEventSchedulesBatch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<int>> RemoveBundleEventSchedulesBatch(
            long bundleId, [FromBody] BundleEventScheduleRemoveRequest request)
        {
            var removed = await bundleEventScheduleService.RemoveBatchAsync(bundleId, request);
            return Ok(removed);
        }
    }
}
