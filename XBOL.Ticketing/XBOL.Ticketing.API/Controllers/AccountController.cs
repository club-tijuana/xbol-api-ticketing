using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(UserManager<User> _userManager) : Controller
    {
        [HttpPost]
        public async Task<ActionResult<bool>> RegisterUser(CreateUserRequest request)
        {
            User newUser = new()
            {
                UserName = request.UserName,
                Email = request.Email
            };

            IdentityResult result = await _userManager.CreateAsync(newUser, request.Password);

            return Ok(true);
        }
    }

    // TODO: Move to DTOs
    public class CreateUserRequest
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
