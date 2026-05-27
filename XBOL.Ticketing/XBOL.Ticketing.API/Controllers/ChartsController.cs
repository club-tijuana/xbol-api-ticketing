using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/charts")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly SeatsIoService _seatsIoService;

        public ChartsController(SeatsIoService seatsIoService)
        {
            _seatsIoService = seatsIoService;
        }

        /// <summary>
        /// Retreive a seats.io chart asynchronously.
        /// </summary>
        /// <param name="chartKey">The unique Id for the chart.</param>
        /// <returns>An ActionResult containing the object reprseenting the chart.</returns>
        [HttpGet("{chartKey}")]
        [EndpointName("GetChartByKeyAsync")]
        [ProducesResponseType(typeof(ChartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ChartResponse>> GetChartByKeyAsync([FromRoute] string chartKey)
        {
            var result = await _seatsIoService.RetrieveMapChartAsync(chartKey);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(new ChartResponse
            {
                Id = result.Id,
                Key = result.Key,
                Name = result.Name,
                Status = result.Status,
                Archived = result.Archived,
                PublishedVersionThumbnailUrl = result.PublishedVersionThumbnailUrl,
                DraftVersionThumbnailUrl = result.DraftVersionThumbnailUrl,
                VenueType = result.VenueType,
                Validation = new ChartValidationResponse
                {
                    Errors = result.Validation.Errors,
                    Warnings = result.Validation.Warnings
                }
            });
        }

        /// <summary>
        /// Retrieves a list of seats.io charts asynchronously
        /// </summary>
        /// <returns>An ActionResult containing a list of objects representing the available charts.</returns>
        [HttpGet]
        [EndpointName("GetChartsAsync")]
        [ProducesResponseType(typeof(List<ChartResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ChartResponse>>> GetChartsAsync()
        {
            var result = await _seatsIoService.RetrieveMapChartsAsync();

            return Ok(result.Select(c => new ChartResponse
            {
                Id = c.Id,
                Key = c.Key,
                Name = c.Name,
                Status = c.Status,
                Archived = c.Archived,
                PublishedVersionThumbnailUrl = c.PublishedVersionThumbnailUrl,
                DraftVersionThumbnailUrl = c.DraftVersionThumbnailUrl,
                VenueType = c.VenueType,
                Validation = new ChartValidationResponse
                {
                    Errors = c.Validation.Errors,
                    Warnings = c.Validation.Warnings
                }
            }).ToList());
        }
    }
}
