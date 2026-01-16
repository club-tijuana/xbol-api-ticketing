using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Services.Category;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<string>> GetCategories([FromServices] CategoryService service)
        {
            return Ok(service.GetCategoryNames());
        }
    }
}
