using MailCrafter.Domain;
using MailCrafter.Services;
using MailCrafter.Services.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;
using System.Text.Json;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class CustomGroupController : Controller
    {
        private readonly ICustomGroupService _customGroupService;
        private readonly ILogger<CustomGroupController> _logger;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IAppUserService _appUserService;
        private readonly IEmailJobService _emailJobService;

        public CustomGroupController(ICustomGroupService customGroupService, ILogger<CustomGroupController> logger, IEmailTemplateService emailTemplateService, IAppUserService appUserService, IEmailJobService emailJobService)
        {
            _customGroupService = customGroupService;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
            _appUserService = appUserService;
            _emailJobService = emailJobService;
        }

        // GET: View list of groups
        [HttpGet]
        [Route("management/groups")]
        public async Task<IActionResult> ManageGroups()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var groups = await _customGroupService.GetGroupsByUserId(userId);
            
            // Add diagnostic logging
            Console.WriteLine($"Found {groups.Count} groups for user {userId}");
            
            // If there are no groups, we add a message
            if (!groups.Any())
            {
                TempData["InfoMessage"] = "You don't have any groups yet. Click 'Add New Group' to create your first group.";
            }
            
            return View(groups);
        }

        // GET: Add new group
        [HttpGet]
        [Route("management/groups/add")]
        public IActionResult AddGroup()
        {
            return View();
        }

        // POST: Add new group
        [HttpPost]
        [Route("management/groups/add")]
        public async Task<IActionResult> AddGroup([FromForm] string GroupName, [FromForm] List<string> emails, [FromForm] List<string> fieldNames, [FromForm] List<string> fieldValues)
        {
            if (string.IsNullOrWhiteSpace(GroupName) || emails.Count == 0)
            {
                TempData["ErrorMessage"] = "Group name and at least one email are required.";
                return View();
            }

            var model = new CustomGroupEntity
            {
                GroupName = GroupName,
                CustomFieldsList = new List<Dictionary<string, string>>()
            };

            // Determine number of recipients
            int recipientCount = emails.Count;
            
            // Total number of fields per recipient (including Email)
            int fieldsPerRecipient = (fieldNames.Count / recipientCount) + 1;

            // Create a dictionary for each recipient
            for (int i = 0; i < recipientCount; i++)
            {
                var recipient = new Dictionary<string, string>
                {
                    { "Email", emails[i] }
                };

                // Add other fields for this recipient
                for (int j = 0; j < fieldNames.Count / recipientCount; j++)
                {
                    int fieldIndex = (i * (fieldNames.Count / recipientCount)) + j;
                    if (fieldIndex < fieldNames.Count && fieldIndex < fieldValues.Count)
                    {
                        recipient[fieldNames[fieldIndex]] = fieldValues[fieldIndex];
                    }
                }
                
                model.CustomFieldsList.Add(recipient);
            }

            model.UserID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.ID = ObjectId.GenerateNewId().ToString();

            var result = await _customGroupService.Create(model);

            if (result.IsSuccessful)
            {
                TempData["SuccessMessage"] = "Group added successfully.";
                return RedirectToAction("ManageGroups");
            }

            TempData["ErrorMessage"] = "Failed to add group.";
            return View();
        }

        // GET: View group details
        [HttpGet]
        [Route("management/groups/view/{id}")]
        public async Task<IActionResult> ViewGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "Group not found or you do not have access.";
                return RedirectToAction("ManageGroups");
            }
            return View(group);
        }

        // GET: Edit group
        [HttpGet]
        [Route("management/groups/edit/{id}")]
        public async Task<IActionResult> EditGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "Group not found or you do not have access.";
                return RedirectToAction("ManageGroups");
            }
            return View(group);
        }

        // POST: Update group
        [HttpPost]
        [Route("management/groups/edit/{id}")]
        public async Task<IActionResult> EditGroup(string id, string groupName, string emails, string fieldNames, string fieldValues)
        {
            try
            {
                var updatedEntity = await _customGroupService.GetById(id);
                if (updatedEntity == null)
                {
                    TempData["ErrorMessage"] = "Group not found.";
                    return RedirectToAction(nameof(ManageGroups));
                }

                updatedEntity.GroupName = groupName;
                updatedEntity.UpdatedAt = DateTime.UtcNow;

                // Clear existing custom fields
                updatedEntity.CustomFieldsList.Clear();

                // Parse the recipients' data
                var emailsList = JsonSerializer.Deserialize<List<string>>(emails);
                var fieldNamesList = JsonSerializer.Deserialize<List<string>>(fieldNames);
                var fieldValuesList = JsonSerializer.Deserialize<List<string>>(fieldValues);

                // Determine number of recipients
                int recipientCount = emailsList.Count;
                
                // Calculate fields per recipient (excluding Email)
                int fieldsPerRecipient = fieldNamesList.Count / recipientCount;

                // Create a dictionary for each recipient
                for (int i = 0; i < recipientCount; i++)
                {
                    var recipient = new Dictionary<string, string>
                    {
                        { "Email", emailsList[i] }
                    };

                    // Add other fields for this recipient
                    for (int j = 0; j < fieldsPerRecipient; j++)
                    {
                        int fieldIndex = (i * fieldsPerRecipient) + j;
                        if (fieldIndex < fieldNamesList.Count && fieldIndex < fieldValuesList.Count)
                        {
                            // Clean up the field name and value
                            var cleanFieldName = fieldNamesList[fieldIndex].Replace("[", "").Replace("]", "").Replace("\"", "");
                            var cleanFieldValue = fieldValuesList[fieldIndex].Replace("[", "").Replace("]", "").Replace("\"", "");
                            recipient[cleanFieldName] = cleanFieldValue;
                        }
                    }
                    
                    updatedEntity.CustomFieldsList.Add(recipient);
                }

                await _customGroupService.Update(updatedEntity);
                TempData["SuccessMessage"] = "Group updated successfully.";
                return RedirectToAction(nameof(ManageGroups));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                TempData["ErrorMessage"] = "An error occurred while updating the group.";
                return RedirectToAction(nameof(ManageGroups));
            }
        }

        // GET: Delete group
        [HttpGet]
        [Route("management/groups/delete/{id}")]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "Group not found or you do not have access.";
                return RedirectToAction("ManageGroups");
            }
            return View(group);
        }

        // POST: Confirm delete group
        [HttpPost]
        [Route("management/groups/delete/{id}")]
        public async Task<IActionResult> DeleteGroupConfirmed(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "Group not found or you do not have access.";
                return RedirectToAction("ManageGroups");
            }

            var result = await _customGroupService.Delete(id);
            if (result.IsSuccessful)
            {
                TempData["SuccessMessage"] = "Group deleted successfully.";
                return RedirectToAction("ManageGroups");
            }

            TempData["ErrorMessage"] = "Failed to delete group.";
            return RedirectToAction("ManageGroups");
        }

        // GET: Send personalized email to a group
        [HttpGet]
        [Route("management/groups/send-email/{id}")]
        public async Task<IActionResult> SendEmail(string id)
        {
            var group = await _customGroupService.GetById(id);
            if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "Group not found or you do not have access.";
                return RedirectToAction("ManageGroups");
            }
            
            ViewBag.Group = group;
            
            // Get user's email templates for the dropdown
            var templates = await _emailTemplateService.GetByUserID(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.Templates = templates;
            
            // Get user's email accounts for the dropdown
            var user = await _appUserService.GetById(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.EmailAccounts = user.EmailAccounts;
            
            var model = new Models.SendEmailViewModel
            {
                GroupId = id
            };
            
            return View(model);
        }
        
        // POST: Send personalized email to a group
        [HttpPost]
        [Route("management/groups/send-email/{id}")]
        public async Task<IActionResult> SendEmail(string id, Models.SendEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate ViewBag data
                var group = await _customGroupService.GetById(id);
                var templates = await _emailTemplateService.GetByUserID(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _appUserService.GetById(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                ViewBag.Group = group;
                ViewBag.Templates = templates;
                ViewBag.EmailAccounts = user.EmailAccounts;
                
                return View(model);
            }
            
            try
            {
                var group = await _customGroupService.GetById(id);
                if (group == null || group.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
                {
                    TempData["ErrorMessage"] = "Group not found or you do not have access.";
                    return RedirectToAction("ManageGroups");
                }
                
                var template = await _emailTemplateService.GetById(model.TemplateId);
                if (template == null || template.UserID != User.FindFirstValue(ClaimTypes.NameIdentifier))
                {
                    TempData["ErrorMessage"] = "Template not found or you do not have access.";
                    return RedirectToAction("SendEmail", new { id });
                }
                
                // Create a new job
                var job = new EmailJobEntity
                {
                    UserID = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Name = $"Personalized Email to {group.GroupName}",
                    TemplateID = model.TemplateId,
                    TemplateName = template.Name,
                    FromEmail = model.FromEmail,
                    GroupID = id,
                    IsPersonalized = true,
                    Status = "Pending",
                    CC = model.CC ?? new List<string>(),
                    Bcc = model.Bcc ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                var result = await _emailJobService.Create(job);
                
                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Personalized email job created successfully.";
                    return RedirectToAction("Details", "Jobs", new { id = job.ID });
                }
                
                TempData["ErrorMessage"] = "Failed to create job.";
                return RedirectToAction("SendEmail", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending personalized email to group {GroupId}", id);
                TempData["ErrorMessage"] = "An error occurred while creating the email job.";
                return RedirectToAction("SendEmail", new { id });
            }
        }
    }
}