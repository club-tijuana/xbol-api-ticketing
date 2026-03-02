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

        /// <summary>
        /// Retrieves a list of category names available in the system.
        /// </summary>
        /// <returns>An object containing the names of all available categories. The list will be empty if no
        /// categories are found.</returns>
        [HttpGet("names")]
        [EndpointName("GetCategoriesNames")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
        public ActionResult<List<string>> GetCategoriesNames()
        {
            return Ok(_categoryService.GetCategoryNames());
        }
    }
}
