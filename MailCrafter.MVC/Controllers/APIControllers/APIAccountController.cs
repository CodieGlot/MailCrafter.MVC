using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using MailCrafter.Domain;
using MailCrafter.Services;
using MailCrafter.Utils.Helpers;
using MailCrafter.MVC.Models.AppUser;

namespace MailCrafter.MVC.Controllers.APIControllers
{
    [Route("api/auth")]
    [ApiController]
    public class APIAccountController : ControllerBase
    {
        private readonly IAppUserService _userService;

        public APIAccountController(IAppUserService userService)
        {
            _userService = userService;
        }

        [HttpOptions("login")]
        public IActionResult LoginOptions()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            Response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userService.GetByUsernameOrEmail(model.Username);
            if (user != null && EncryptHelper.Verify(model.Password, user.Password))
            {
                // Generate a simple token (in production, you'd use JWT)
                string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                
                return Ok(new { 
                    token = token, 
                    success = true,
                    username = user.Username,
                    email = user.Email
                });
            }

            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AppUserEntity model)
        {
            // Check if user already exists
            var existingUser = await _userService.GetByUsernameOrEmail(model.Username);
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "Username or email already exists" });
            }

            // Create new user with encrypted password
            model.Password = EncryptHelper.HashPassword(model.Password);
            var result = await _userService.Create(model);

            if (result.IsSuccessful)
            {
                return Ok(new { success = true, message = "User registered successfully" });
            }

            return BadRequest(new { success = false, message = "Registration failed" });
        }
    }
}
