using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class EmailAccountController : Controller
    {
        private readonly IAppUserService _userService;

        public EmailAccountController(IAppUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Route("management/email-accounts/add")]
        public IActionResult AddEmailAccount()
        {
            return View();
        }

        [HttpPost]
        [Route("management/email-accounts/add")]
        public async Task<IActionResult> AddEmailAccount([FromBody] EmailAccount emailAccount)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 

                var result = await _userService.AddEmailAccount(userId, emailAccount);

                if (result.IsSuccessful)
                {
                    return Ok();
                }

                ModelState.AddModelError(string.Empty, "Failed to add email account.");
            }

            return BadRequest(new { errors = ModelState });
        }

        [HttpGet]
        [Route("management/email-accounts")]
        public async Task<IActionResult> ManageEmailAccounts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var emailAccounts = await _userService.GetEmailAccountsOfUser(userId);
            return View(emailAccounts);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveEmailAccount([FromQuery] string email)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var result = await _userService.RemoveEmailAccount(userId, email);
            
            if (result.IsAcknowledged)
            {
                return Ok(new { message = "Email account removed successfully." });
            }

            return BadRequest(new { message = "Failed to remove email account." });
        }


        [HttpPut("update-password")]
        public async Task<IActionResult> UpdateEmailPassword([FromQuery] string? email, [FromBody] string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "Password is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userService.UpdateEmailPassword(userId, email ?? string.Empty, newPassword);
            if (result.MatchedCount > 0)
                return Ok(new { message = "Password updated successfully." });

            return BadRequest(new { message = "Failed to update password." });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("EmailAccount/WhatIsAppPassword")]
        public IActionResult WhatIsAppPassword()
        {
            return View();
        }
    }
}
