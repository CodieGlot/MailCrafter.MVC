using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class CustomGroupController : Controller
    {
        private readonly ICustomGroupService _customGroupService;

        public CustomGroupController(ICustomGroupService customGroupService)
        {
            _customGroupService = customGroupService;
        }

        [HttpGet]
        [Route("management/groups")]
        public async Task<IActionResult> ViewGroups()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _customGroupService.GetGroupsByUserId(userId);
            return View(groups ?? new List<CustomGroupEntity>());
        }


        [HttpGet]
        [Route("management/groups/add")]
        public IActionResult AddGroup()
        {
            return View();
        }

        [HttpPost]
        [Route("management/groups/add")]
        public async Task<IActionResult> AddGroup([FromBody] CustomGroupEntity model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { errors = ModelState });
            }

            model.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.ID = ObjectId.GenerateNewId().ToString();

            var result = await _customGroupService.Create(model);

            if (result.IsSuccessful)
            {
                return Ok(new { redirectUrl = Url.Action("ViewGroups") });
            }

            ModelState.AddModelError(string.Empty, "Failed to add group.");
            return BadRequest(new { errors = ModelState });
        }

    }
}
