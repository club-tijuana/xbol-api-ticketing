using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get([FromServices] IWebHostEnvironment env) => Ok($"{env.ApplicationName} - {env.EnvironmentName} OK 👍");

        [HttpGet("role/{id}")]
        public async Task<IActionResult> GetRole([FromServices] RoleService service, [FromRoute] Guid id)
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
    }
}