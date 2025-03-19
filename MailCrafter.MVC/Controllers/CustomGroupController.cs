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

        // GET: Xem danh sách các nhóm (List)
        [HttpGet]
        [Route("management/groups")]
        public async Task<IActionResult> ManageGroups()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _customGroupService.GetGroupsByUserId(userId);
            return View(groups);
        }

        // GET: Thêm nhóm mới (Add)
        [HttpGet]
        [Route("management/groups/add")]
        public IActionResult AddGroup()
        {
            return View();
        }

        // POST: Thêm nhóm mới
        [HttpPost]
        [Route("management/groups/add")]
        public async Task<IActionResult> AddGroup([FromForm] string GroupName, [FromForm] List<string> fieldNames, [FromForm] List<string> fieldValues, [FromForm] List<string> emails)
        {
            if (string.IsNullOrWhiteSpace(GroupName) || fieldNames.Count == 0 || fieldValues.Count == 0 || emails.Count == 0)
            {
                return BadRequest(new { message = "Group name and custom fields are required." });
            }

            var model = new CustomGroupEntity
            {
                GroupName = GroupName,
                CustomFieldsList = new List<Dictionary<string, string>>()
            };

            for (int i = 0; i < fieldNames.Count; i++)
            {
                var customField = new Dictionary<string, string>
                {
                    { "fieldName", fieldNames[i] },
                    { "fieldValue", fieldValues[i] },
                    { "Email", emails[i] }
                };
                model.CustomFieldsList.Add(customField);
            }

            model.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.ID = ObjectId.GenerateNewId().ToString();

            var result = await _customGroupService.Create(model);

            if (result.IsSuccessful)
            {
                return RedirectToAction("ManageGroups");
            }

            return BadRequest(new { message = "Failed to add group." });
        }

        // GET: Xem chi tiết một nhóm (View)
        [HttpGet]
        [Route("management/groups/view/{id}")]
        public async Task<IActionResult> ViewGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return BadRequest(new { message = "Group not found or you do not have access." });
            }
            return View(group);
        }

        // GET: Chỉnh sửa một nhóm (Edit)
        [HttpGet]
        [Route("management/groups/edit/{id}")]
        public async Task<IActionResult> EditGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return BadRequest(new { message = "Group not found or you do not have access." });
            }
            return View(group);
        }

        // POST: Cập nhật một nhóm
        [HttpPost]
        [Route("management/groups/edit/{id}")]
        public async Task<IActionResult> EditGroup(string id, [FromForm] string GroupName, [FromForm] List<string> fieldNames, [FromForm] List<string> fieldValues, [FromForm] List<string> emails)
        {
            if (string.IsNullOrWhiteSpace(GroupName) || fieldNames.Count == 0 || fieldValues.Count == 0 || emails.Count == 0)
            {
                return BadRequest(new { message = "Group name and custom fields are required." });
            }

            var existingGroup = await _customGroupService.GetById(id);
            if (existingGroup == null || existingGroup.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return BadRequest(new { message = "Group not found or you do not have access." });
            }

            var updatedEntity = new CustomGroupEntity
            {
                ID = id,
                GroupName = GroupName,
                CustomFieldsList = new List<Dictionary<string, string>>(),
                UserID = existingGroup.UserID,
                CreatedAt = existingGroup.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            for (int i = 0; i < fieldNames.Count; i++)
            {
                var customField = new Dictionary<string, string>
                {
                    { "fieldName", fieldNames[i] },
                    { "fieldValue", fieldValues[i] },
                    { "Email", emails[i] }
                };
                updatedEntity.CustomFieldsList.Add(customField);
            }

            var result = await _customGroupService.Update(updatedEntity);
            if (result.IsSuccessful)
            {
                return RedirectToAction("ManageGroups");
            }

            return BadRequest(new { message = "Failed to update group." });
        }

        // GET: Xóa một nhóm (Delete - Xác nhận)
        [HttpGet]
        [Route("management/groups/delete/{id}")]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return BadRequest(new { message = "Group not found or you do not have access." });
            }
            return View(group);
        }

        // POST: Xác nhận xóa một nhóm
        [HttpPost]
        [Route("management/groups/delete/{id}")]
        public async Task<IActionResult> DeleteGroupConfirmed(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return BadRequest(new { message = "Group not found or you do not have access." });
            }

            var result = await _customGroupService.Delete(id);
            if (result.IsSuccessful)
            {
                return RedirectToAction("ManageGroups");
            }

            return BadRequest(new { message = "Failed to delete group." });
        }
    }
}