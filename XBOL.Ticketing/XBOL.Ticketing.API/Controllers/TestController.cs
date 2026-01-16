using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Identity;
using XBOL.Ticketing.Services.RulesEngine;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("api/tests")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<TestResultObject> GetApiEnvironmet([FromServices] IWebHostEnvironment env) =>
            Ok(new TestResultObject
            {
                Result = $"{env.ApplicationName} - {env.EnvironmentName} OK 👍",
            });

        /// <summary>
        /// Calculates dynamic ticket prices for the specified event using the provided rules engine service.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event for which to calculate dynamic prices.</param>
        /// <returns>An <see cref="IActionResult"/> containing the calculated pricing information if successful; otherwise, a bad
        /// request result.</returns>
        [HttpPost("dynamic-pricing/{eventId}")]
        public async Task<IActionResult> CalculatePricesAsync([FromServices] RulesEngineService service, [FromRoute] long eventId)
        {
            try
            {
                var response = await service.ExecuteDynamicPricingAsync(eventId);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public class TestResultObject
        {
            public required string Result { get; set; }
        }
    }
}
