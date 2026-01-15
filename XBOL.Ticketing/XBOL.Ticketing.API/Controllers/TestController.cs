using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Services.Identity;
using XBOL.Ticketing.Services.RulesEngine;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<TestResultObject> Get([FromServices] IWebHostEnvironment env) => Ok(new TestResultObject { Result = $"{env.ApplicationName} - {env.EnvironmentName} OK 👍" });

        [HttpGet("role/{id}")]
        public async Task<ActionResult<Role?>> GetRole([FromServices] RoleService service, [FromRoute] Guid id)
        {
            try
            {
                var response = await service.GetById(id);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("dynamic-pricing/{eventId}")]
        public async Task<IActionResult> GetRole([FromServices] RulesEngineService service, [FromRoute] long eventId)
        {
            try
            {
                var response = await service.ExecuteDynamicPricing(eventId);
                return Ok(response);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public class TestResultObject
        {
            public string Result { get; set; }
        }
    }
}