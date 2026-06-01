using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.API.Controllers
{
    /// <summary>
    /// Manage passes within a Bundle.
    /// </summary>
    /// <remarks>
    /// A BundlePass represents a single admission pass tied to a Bundle.
    /// Passes can be filtered by status and searched by term.
    /// </remarks>
    [Route("api/bundles/{bundleId:long}/passes")]
    [ApiController]
    [Tags("Bundle Passes")]
    public class BundlePassController(BundlePassService bundlePassService) : ControllerBase
    {
        /// <summary>
        /// Returns a paginated list of passes for a Bundle.
        /// </summary>
        /// <remarks>
        /// Supports filtering by status and free-text search.
        /// </remarks>
        [HttpGet]
        [EndpointName("GetBundlePasses")]
        [ProducesResponseType(typeof(PagedResponse<BundlePassDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<BundlePassDTO>>> GetBundlePasses(
            long bundleId, [FromQuery] BundlePassQueryParams queryParams)
        {
            queryParams.BundleId = bundleId;
            var result = await bundlePassService.GetPagedAsync(queryParams);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single pass by its ID.
        /// </summary>
        [HttpGet("{id:long}")]
        [EndpointName("GetBundlePassById")]
        [ProducesResponseType(typeof(BundlePassDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BundlePassDTO>> GetBundlePassById(long bundleId, long id)
        {
            var pass = await bundlePassService.GetByIdAsync(id);
            if (pass is null) { return NotFound(); }
            return Ok(pass);
        }

        /// <summary>
        /// Creates a new pass for a Bundle.
        /// </summary>
        /// <remarks>
        /// The pass type, price, and optional seat assignment are required.
        /// The calling user is recorded as the creator.
        /// </remarks>
        [HttpPost]
        [EndpointName("CreateBundlePass")]
        [ProducesResponseType(typeof(BundlePassDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BundlePassDTO>> CreateBundlePass(
            long bundleId, [FromBody] BundlePassCreateRequest request)
        {
            request.BundleId = bundleId;
            var userId = GetUserId();
            var pass = await bundlePassService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetBundlePassById), new { bundleId, id = pass.Id }, pass);
        }

        /// <summary>
        /// Updates an existing pass.
        /// </summary>
        /// <remarks>
        /// Supports changing status, suspension reason, price, seat assignment, and delivery mode.
        /// </remarks>
        [HttpPut("{id:long}")]
        [EndpointName("UpdateBundlePass")]
        [ProducesResponseType(typeof(BundlePassDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BundlePassDTO>> UpdateBundlePass(
            long bundleId, long id, [FromBody] BundlePassUpdateRequest request)
        {
            var userId = GetUserId();
            var pass = await bundlePassService.UpdateAsync(id, request, userId);
            if (pass is null) { return NotFound(); }
            return Ok(pass);
        }

        /// <summary>
        /// Deletes a pass by its ID.
        /// </summary>
        [HttpDelete("{id:long}")]
        [EndpointName("DeleteBundlePass")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBundlePass(long bundleId, long id)
        {
            var deleted = await bundlePassService.DeleteAsync(id);
            if (!deleted) { return NotFound(); }
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
