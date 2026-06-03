using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/client-prices")]
    [ApiController]
    [Obsolete("This controller is being deprecated in favor of the standard PriceController. For now we are only using it for backward compatibility.")]
    public class ClientPricesController : ControllerBase
    {
        private readonly ClientPriceService _clientPriceService;

        public ClientPricesController(ClientPriceService clientPriceService)
        {
            _clientPriceService = clientPriceService;
        }

        /// <summary>
        /// Gets the seat availability based on the provided filters. This endpoint is used to determine which seats are available for reservation based on the specified criteria such as season, schedule, section, zone, and price range.
        /// </summary>
        /// <param name="filters"></param>
        [HttpGet("seat-availability")]
        [ProducesResponseType(typeof(SeatAvailabilityResponse), StatusCodes.Status200OK)]
        [EndpointName("GetSeatAvailabilityAsync")]
        public async Task<ActionResult<SeatAvailabilityResponse>> GetSeatAvailabilityAsync([FromBody] ReservationFiltersRequest filters)
        {
            var result = await _clientPriceService.GetSeatAvailabilityAsync(filters);

            return Ok(result);
        }

        /// <summary>
        /// Gets the prices for the specified sections based on their sale type and reference ID.
        /// </summary>
        /// <param name="saleType"></param>
        /// <param name="referenceId"></param>
        [HttpGet("section-prices")]
        [ProducesResponseType(typeof(List<SectionPriceResponse>), StatusCodes.Status200OK)]
        [EndpointName("GetSectionPricesAsync")]
        public async Task<ActionResult<List<SectionPriceResponse>>> GetSectionPricesAsync([FromQuery] SaleType saleType, [FromQuery] long referenceId)
        {
            var result = await _clientPriceService.GetSectionPricesAsync(saleType, referenceId);

            return Ok(result);
        }
    }
}
