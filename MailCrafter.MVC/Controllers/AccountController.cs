using MailCrafter.Domain;
using MailCrafter.MVC.Models.AppUser;
using MailCrafter.Services;
using MailCrafter.Utils.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IAppUserService _userService;

        public AccountController(ILogger<AccountController> logger, IAppUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] AppUserEntity model)
        {
            var user = await _userService.GetByUsernameOrEmail(model.Username);
            if (user != null && EncryptHelper.Verify(model.Password, user.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.ID),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                return Ok(new { redirectUrl = Url.Action("Index", "Home") });
            }

            return Unauthorized(new { message = "Invalid login attempt." });
        }

        [HttpGet]
        [Route("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UserRegistration model)
        {
            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUserByUsername = await _userService.GetByUsernameOrEmail(model.Username);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError("Username", "Username is already taken.");
                    return BadRequest(new { errors = ModelState });
                }

                // Check if email already exists
                var existingUserByEmail = await _userService.GetByUsernameOrEmail(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return BadRequest(new { errors = ModelState });
                }

                var user = new AppUserEntity
                {
                    ID = ObjectId.GenerateNewId().ToString(),
                    Username = model.Username,
                    Email = model.Email,
                    Password = EncryptHelper.HashPassword(model.Password)
                };

                var result = await _userService.Create(user);

                if (result.IsSuccessful)
                {
                    return Ok(new { redirectUrl = Url.Action("Login") });
                }

                ModelState.AddModelError(string.Empty, "Registration failed.");
            }

            return BadRequest(new { errors = ModelState });
        }

        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return Ok(new { redirectUrl = Url.Action("Login") });
        }

        [HttpGet]
        [Route("forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            var user = await _userService.GetByUsernameOrEmail(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(new { message = "If your email is registered, you will receive a password reset link." });
            }

            // Generate a password reset token
            var token = Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token, email = user.Email }, Request.Scheme);

            // TODO: Send email with reset link
            // For now, we'll just return the link in the response
            return Ok(new { 
                message = "If your email is registered, you will receive a password reset link.",
                resetLink = resetLink // Remove this in production
            });
        }

        [HttpGet]
        [Route("reset-password")]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Invalid password reset link." });
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest(new { message = "All fields are required." });
            }

            var user = await _userService.GetByUsernameOrEmail(model.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid password reset request." });
            }

            // TODO: Validate token
            // For now, we'll just update the password
            user.Password = EncryptHelper.HashPassword(model.NewPassword);
            var result = await _userService.Update(user);

            if (result.IsSuccessful)
            {
                return Ok(new { message = "Password has been reset successfully." });
            }

            return BadRequest(new { message = "Failed to reset password." });
        }

        //public async Task<ApiPageQueryResponse<AppUserEntity>> GetAllAppUsers(ApiPageQueryRequest request)
        //{
        //    var data = await _userService.GetPageQueryDataAsync(request.ToPageQueryDTO());
        //    return new ApiPageQueryResponse<AppUserEntity>(data, request);
        //}
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
