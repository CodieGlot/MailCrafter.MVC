using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class UpdateEmailPasswordController : Controller
    {
        private readonly IAppUserService _userService;

        public UpdateEmailPasswordController(IAppUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Route("management/email-accounts/update-password/{email}")]
        public IActionResult UpdateEmailPassword(string email)
        {
            return View(new { Email = email });
        }

        [HttpPost]
        [Route("management/email-accounts/update-password/{email}")]
        public async Task<IActionResult> UpdateEmailPassword(string email, [FromBody] string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError(string.Empty, "Password cannot be empty.");
                return BadRequest(new { errors = ModelState });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _userService.UpdateEmailPassword(userId, email, newPassword);

            if (result.IsSuccessful)
            {
                return Ok(new { message = "Password updated successfully." });
            }

            ModelState.AddModelError(string.Empty, "Failed to update password.");
            return BadRequest(new { errors = ModelState });
        }
    }
}
