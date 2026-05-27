using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/prices")]
    [ApiController]
    public class PricesController : ControllerBase
    {
        private readonly PriceService _priceService;

        public PricesController(PriceService priceService)
        {
            _priceService = priceService;
        }

        /// <summary>
        /// Gets the Prices for an Event, Season or Bundle based on the active Price List. If there are no active price lists or items, it returns 204 No Content.
        /// </summary>
        /// <param name="referenceType">Reference type (Event, Season, Bundle)</param>
        /// <param name="referenceId">Id of the reference</param>
        /// <returns>Seats.io price structure or 204 if no data is available</returns>
        [HttpGet]
        [EndpointName("GetSeatsIoPricesAsync")]
        [ProducesResponseType(typeof(List<SeatsIoPriceDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> GetSeatsIoPrices([FromQuery] SaleType referenceType, [FromQuery] long referenceId)
        {
            var prices = await _priceService.GetSeatsIoPricesAsync(referenceType, referenceId);

            if (prices == null || !prices.Any())
            {
                return NoContent();
            }

            return Ok(prices);
        }
    }
}
