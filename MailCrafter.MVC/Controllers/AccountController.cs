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

        //public async Task<ApiPageQueryResponse<AppUserEntity>> GetAllAppUsers(ApiPageQueryRequest request)
        //{
        //    var data = await _userService.GetPageQueryDataAsync(request.ToPageQueryDTO());
        //    return new ApiPageQueryResponse<AppUserEntity>(data, request);
        //}
    }
}
