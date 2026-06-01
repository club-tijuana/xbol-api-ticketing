using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services.Bundle;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/bundles")]
    [ApiController]
    [Tags("Bundles")]
    public class BundleController(BundleService bundleService) : ControllerBase
    {
        /// <summary>
        /// Returns a paginated list of bundles, optionally filtered by search term.
        /// </summary>
        [HttpGet]
        [EndpointName("GetBundles")]
        [ProducesResponseType(typeof(PagedResponse<BundleDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResponse<BundleDTO>>> GetBundles(
            [FromQuery] BundleQueryParams queryParams)
        {
            var result = await bundleService.GetPagedAsync(queryParams);
            return Ok(result);
        }

        /// <summary>
        /// Returns a single bundle by its ID.
        /// </summary>
        [HttpGet("{id:long}")]
        [EndpointName("GetBundleById")]
        [ProducesResponseType(typeof(BundleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BundleDTO>> GetBundleById(long id)
        {
            var bundle = await bundleService.GetByIdAsync(id);
            if (bundle is null) { return NotFound(); }
            return Ok(bundle);
        }

        /// <summary>
        /// Creates a new bundle.
        /// </summary>
        [HttpPost]
        [EndpointName("CreateBundle")]
        [ProducesResponseType(typeof(BundleDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult<BundleDTO>> CreateBundle(
            [FromBody] BundleCreateRequest request)
        {
            var userId = GetUserId();
            var bundle = await bundleService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetBundleById), new { id = bundle.Id }, bundle);
        }

        /// <summary>
        /// Updates an existing bundle.
        /// </summary>
        [HttpPut("{id:long}")]
        [EndpointName("UpdateBundle")]
        [ProducesResponseType(typeof(BundleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BundleDTO>> UpdateBundle(
            long id, [FromBody] BundleUpdateRequest request)
        {
            var userId = GetUserId();
            var bundle = await bundleService.UpdateAsync(id, request, userId);
            if (bundle is null) { return NotFound(); }
            return Ok(bundle);
        }

        /// <summary>
        /// Deletes a bundle by its ID.
        /// </summary>
        [HttpDelete("{id:long}")]
        [EndpointName("DeleteBundle")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteBundle(long id)
        {
            var deleted = await bundleService.DeleteAsync(id);
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
