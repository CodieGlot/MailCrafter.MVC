using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class AdminAccountUserController : Controller
    {
        private readonly IAppUserService _userService;

        public AdminAccountUserController(IAppUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("management/get-username-or-email")]
        public async Task<IActionResult> GetByUsernameOrEmail([FromQuery] string usernameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
            {
                ViewBag.ErrorMessage = "Username or email is required.";
                return View("ManagementAccountUser");
            }

            var user = await _userService.GetByUsernameOrEmail(usernameOrEmail);
            if (user == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                return View("ManagementAccountUser");
            }

            return View("ManagementAccountUser", user);
        }


        [HttpGet("management/all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllAppUsers();
            if (users == null || !users.Any())
                return NotFound(new { message = "No users found." });

            var model = users.Select(u => $"{u.ID}|{u.Username} - {u.Email}").ToList();
            return View("ManagementAccountUser", model);
        }
        [HttpDelete("management/remove/{id}")]
        public async Task<IActionResult> RemoveUserAccount(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { success = false, message = "User ID is required." });

            var user = await _userService.GetById(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            var loggedInUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (id == loggedInUserId)
                return BadRequest(new { success = false, message = "You cannot delete your own account." });

            var result = await _userService.Delete(id);
            if (result?.IsAcknowledged == true)
                return Ok(new { success = true, message = "User account removed successfully.", username = user.Username });

            return BadRequest(new { success = false, message = "Failed to remove user account." });
        }

        [HttpPut("management/update-user-account/{id}")]
        public async Task<IActionResult> UpdateAccountUser(string id, [FromBody] Dictionary<string, string> body)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { success = false, message = "User ID is required." });

            if (body == null)
                return BadRequest(new { success = false, message = "Invalid request body." });

            var user = await _userService.GetById(id);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            bool isUpdated = false;

            if (body.TryGetValue("newEmail", out var newEmail) && !string.IsNullOrWhiteSpace(newEmail))
            {
                user.Email = newEmail;
                isUpdated = true;
            }

            if (body.TryGetValue("newUserName", out var newUserName) && !string.IsNullOrWhiteSpace(newUserName))
            {
                user.Username = newUserName;
                isUpdated = true;
            }

            if (body.TryGetValue("newPassword", out var newPassword) && !string.IsNullOrWhiteSpace(newPassword))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                isUpdated = true;
            }

            if (!isUpdated)
                return BadRequest(new { success = false, message = "No valid fields to update." });

            var result = await _userService.Update(user);
            if (result?.MatchedCount > 0 && result?.ModifiedCount > 0)
                return Ok(new { success = true, message = "User account updated successfully.", username = user.Username, email = user.Email });

            return BadRequest(new { success = false, message = "Failed to update user account." });
        }
    }
}
