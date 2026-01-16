using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Services.RulesEngine;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("api/tests")]
    public class TestController : ControllerBase
    {
        private readonly RulesEngineService _rulesEngineService;

        public TestController(RulesEngineService rulesEngineService)
        {
            _rulesEngineService = rulesEngineService;
        }

        [HttpGet]
        [EndpointName("GetApiEnvironment")]
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
        [EndpointName("CalculatePricesAsync")]
        public async Task<IActionResult> CalculatePricesAsync([FromRoute] long eventId)
        {
            try
            {
                var response = await _rulesEngineService.ExecuteDynamicPricingAsync(eventId);
                return Ok(response);
            }
            catch (Exception)
            {
                // TODO: An exception is not a bad request. Handle exceptions properly and return appropriate status codes.
                return BadRequest();
            }
        }

        public class TestResultObject
        {
            public required string Result { get; set; }
        }
    }
}
