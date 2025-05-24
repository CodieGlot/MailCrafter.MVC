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

        [HttpPost]
        [Route("api/account/login")]
        public async Task<IActionResult> ApiLogin([FromBody] AppUserEntity model)
        {
            var user = await _userService.GetByUsernameOrEmail(model.Username);
            if (user != null && EncryptHelper.Verify(model.Password, user.Password))
            {
                // Generate a simple token (in production, you'd use JWT)
                string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                
                // In a real implementation, you would store this token somewhere
                // and validate it on protected endpoints
                
                return Ok(new { token = token, success = true });
            }

            return Unauthorized(new { success = false, message = "Invalid credentials" });
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
                    new Claim(ClaimTypes.NameIdentifier, user.ID)
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

        //public async Task<ApiPageQueryResponse<AppUserEntity>> GetAllAppUsers(ApiPageQueryRequest request)
        //{
        //    var data = await _userService.GetPageQueryDataAsync(request.ToPageQueryDTO());
        //    return new ApiPageQueryResponse<AppUserEntity>(data, request);
        //}
    }
}
