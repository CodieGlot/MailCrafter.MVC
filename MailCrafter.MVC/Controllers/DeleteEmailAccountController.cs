using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MailCrafter.MVC.Controllers
{
    public class DeleteEmailAccountController : Controller
    {
        private readonly ILogger<DeleteEmailAccountController> _logger;
        private readonly IAppUserService _userService;

        public DeleteEmailAccountController(ILogger<DeleteEmailAccountController> logger, IAppUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        [Route("management/email-accounts/delete")]
        public async Task<IActionResult> DeleteEmailAccount([FromBody] string email)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var result = await _userService.RemoveEmailAccount(userId, email);

                if (result.IsSuccessful)
                {
                    return Ok(new { message = "Email account deleted successfully." });
                }

                ModelState.AddModelError(string.Empty, "Failed to delete email account.");
            }

            return BadRequest(new { errors = ModelState });
        }
    }
}