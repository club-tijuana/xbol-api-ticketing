using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Charts;
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

            return Ok(ToChartResponse(result));
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

            return Ok(result.Select(ToChartResponse).ToList());
        }

        private static ChartResponse ToChartResponse(Chart chart)
        {
            return new ChartResponse
            {
                Id = chart.Id,
                Key = chart.Key ?? string.Empty,
                Name = chart.Name ?? string.Empty,
                Status = chart.Status ?? string.Empty,
                Archived = chart.Archived,
                PublishedVersionThumbnailUrl = chart.PublishedVersionThumbnailUrl ?? string.Empty,
                DraftVersionThumbnailUrl = chart.DraftVersionThumbnailUrl ?? string.Empty,
                VenueType = chart.VenueType ?? string.Empty,
                Validation = new ChartValidationResponse
                {
                    Errors = chart.Validation?.Errors?.ToList() ?? [],
                    Warnings = chart.Validation?.Warnings?.ToList() ?? []
                }
            };
        }
    }
}
