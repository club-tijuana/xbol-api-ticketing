using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.API.Controllers
{
    /// <summary>
    /// Manage ticket associations for a Bundle Pass.
    /// </summary>
    /// <remarks>
    /// Each pass can hold multiple tickets linked to different event schedules
    /// within the parent Bundle. This controller handles listing, adding,
    /// and removing those ticket associations.
    /// </remarks>
    [Route("api/bundles/{bundleId:long}/passes/{passId:long}/tickets")]
    [ApiController]
    [Tags("Bundle Pass Event Tickets")]
    public class BundlePassEventTicketController(BundlePassEventTicketService bundlePassEventTicketService) : ControllerBase
    {
        /// <summary>
        /// Lists all tickets associated with a pass.
        /// </summary>
        [HttpGet]
        [EndpointName("GetBundlePassEventTickets")]
        [ProducesResponseType(typeof(List<BundlePassEventTicketDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<BundlePassEventTicketDTO>>> GetBundlePassEventTickets(
            long bundleId, long passId)
        {
            var result = await bundlePassEventTicketService.GetByPassAsync(passId);
            return Ok(result);
        }

        /// <summary>
        /// Adds one or more tickets to a pass.
        /// </summary>
        /// <remarks>
        /// Returns the updated list of all tickets on the pass after the addition.
        /// </remarks>
        [HttpPost]
        [EndpointName("AddBundlePassEventTickets")]
        [ProducesResponseType(typeof(List<BundlePassEventTicketDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<List<BundlePassEventTicketDTO>>> AddBundlePassEventTickets(
            long bundleId, long passId, [FromBody] BundlePassEventTicketAddRequest request)
        {
            var result = await bundlePassEventTicketService.AddAsync(passId, request);
            return Ok(result);
        }

        /// <summary>
        /// Removes one or more tickets from a pass.
        /// </summary>
        /// <remarks>
        /// Returns the count of ticket associations actually removed.
        /// </remarks>
        [HttpDelete]
        [EndpointName("RemoveBundlePassEventTickets")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<int>> RemoveBundlePassEventTickets(
            long bundleId, long passId, [FromBody] BundlePassEventTicketRemoveRequest request)
        {
            var removed = await bundlePassEventTicketService.RemoveAsync(passId, request);
            return Ok(removed);
        }
    }
}
