using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Services.Category;

namespace XBOL.Ticketing.API.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("names")]
        public ActionResult<List<string>> GetCategoriesNames()
        {
            return Ok(_categoryService.GetCategoryNames());
        }
    }
}
