using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Charts;
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
        [ProducesResponseType(typeof(Chart), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Chart>> GetChartByKeyAsync([FromRoute] string chartKey)
        {
            var result = await _seatsIoService.RetrieveMapChartAsync(chartKey);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of seats.io charts asynchronously
        /// </summary>
        /// <returns>An ActionResult containing a list of objects representing the available charts.</returns>
        [HttpGet]
        [EndpointName("GetChartsAsync")]
        [ProducesResponseType(typeof(List<Chart>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Chart>>> GetChartsAsync()
        {
            var result = await _seatsIoService.RetrieveMapChartsAsync();

            return Ok(result);
        }
    }
}
