using Microsoft.AspNetCore.Mvc;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/booking")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        [HttpPost]
        [EndpointName("CreateBooking")]
        public async Task<ActionResult<IEnumerable<string>>> CreateBooking([FromBody] BookingRequest request, [FromServices] SeatsIoService seatsIoService)
        {
            ChangeObjectStatusResult result = await seatsIoService.BookSeatsAsync(request);

            return Ok(result.Objects.Select(x => x.Key));
        }
    }
}
