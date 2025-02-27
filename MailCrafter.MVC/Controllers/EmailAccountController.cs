﻿using MailCrafter.Domain;
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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Assuming user ID is stored in the NameIdentifier claim

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
    }
}

