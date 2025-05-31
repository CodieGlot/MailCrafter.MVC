using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IAppUserService _userService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IAppUserService userService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized();
                }

                var user = await _userService.GetById(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userId);
                    return NotFound("User not found");
                }

                // Validate current password if provided
                if (!string.IsNullOrEmpty(request.CurrentPassword))
                {
                    if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                    {
                        _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                        return BadRequest(new { message = "Current password is incorrect" });
                    }
                }

                // Validate email format
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                    if (!System.Text.RegularExpressions.Regex.IsMatch(request.Email, emailPattern))
                    {
                        _logger.LogWarning("Invalid email format: {Email}", request.Email);
                        return BadRequest(new { message = "Invalid email format" });
                    }
                }

                // Check if new email is already taken by another user
                if (request.Email != user.Email)
                {
                    var existingUserWithEmail = await _userService.GetByUsernameOrEmail(request.Email);
                    if (existingUserWithEmail != null && existingUserWithEmail.ID != userId)
                    {
                        _logger.LogWarning("Email {Email} is already taken by another user", request.Email);
                        return BadRequest(new { message = "Email is already taken by another user" });
                    }
                }

                // Check if new username is already taken by another user
                if (request.Username != user.Username)
                {
                    var existingUserWithUsername = await _userService.GetByUsernameOrEmail(request.Username);
                    if (existingUserWithUsername != null && existingUserWithUsername.ID != userId)
                    {
                        _logger.LogWarning("Username {Username} is already taken by another user", request.Username);
                        return BadRequest(new { message = "Username is already taken by another user" });
                    }
                }

                // Update user information
                user.Username = request.Username;
                user.Email = request.Email;

                // Update password if new password is provided
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                _logger.LogInformation("Attempting to update user profile. UserId: {UserId}, Username: {Username}, Email: {Email}", 
                    userId, request.Username, request.Email);

                var result = await _userService.Update(user);
                if (result.IsSuccessful)
                {
                    _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);
                    return Ok(new { message = "Profile updated successfully" });
                }

                _logger.LogError("Failed to update profile for user: {UserId}. Update result: {@UpdateResult}", userId, result);
                return BadRequest(new { message = "Failed to update profile" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { message = "An error occurred while updating profile" });
            }
        }
    }

    public class UpdateProfileRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
} 