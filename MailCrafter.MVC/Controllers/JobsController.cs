using MailCrafter.Domain;
using MailCrafter.Services;
using MailCrafter.Utils.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class JobsController(
        IEmailTemplateService templateService,
        IAppUserService accountService,
        ICustomGroupService groupService,
        MVCTaskQueueInstance taskQueue,
        IAesEncryptionHelper encryptionHelper,
        ILogger<JobsController> logger) : Controller
    {
        private readonly IEmailTemplateService _templateService = templateService;
        private readonly IAppUserService _accountService = accountService;
        private readonly ICustomGroupService _groupService = groupService;
        private readonly MVCTaskQueueInstance _taskQueue = taskQueue;
        private readonly IAesEncryptionHelper _encryptionHelper = encryptionHelper;
        private readonly ILogger<JobsController> _logger = logger;

        [Route("/jobs")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var details = new BasicEmailDetailsModel
            {
                TemplateID = "67b1641f204c8d7bf4162658",
                Recipients = ["minhdqde180271@fpt.edu.vn"],
                FromMail = "codie.technical@gmail.com",
                AppPassword = encryptionHelper.Encrypt("app-password"),
            };

            await taskQueue.EnqueueAsync(WorkerTaskNames.Send_Basic_Email, details);

            return View();
        }

        [HttpPost]
        [Route("api/jobs")]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get template details
                var template = await _templateService.GetById(request.TemplateId);
                if (template == null || template.UserID != userId)
                {
                    return BadRequest("Invalid template selected");
                }

                // Get group details if group is selected
                var recipients = new List<string>();
                if (!string.IsNullOrEmpty(request.GroupId))
                {
                    var group = await _groupService.GetById(request.GroupId);
                    if (group == null || group.UserID != userId)
                    {
                        return BadRequest("Invalid group selected");
                    }
                    // Extract emails from CustomFieldsList
                    recipients = group.CustomFieldsList
                        .Where(field => field.ContainsKey("Email"))
                        .Select(field => field["Email"])
                        .ToList();
                }

                // Create email details
                var details = new BasicEmailDetailsModel
                {
                    TemplateID = request.TemplateId,
                    Recipients = recipients,
                    FromMail = request.FromEmail,
                    AppPassword = _encryptionHelper.Encrypt("app-password"), // You should get this from your email account settings
                };

                // Queue the email sending task
                await _taskQueue.EnqueueAsync(WorkerTaskNames.Send_Basic_Email, details);

                return Ok(new { message = "Email job created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email job");
                return StatusCode(500, "Failed to create email job");
            }
        }

        #region API Endpoints for AJAX

        /// <summary>
        /// API endpoint to get email templates
        /// </summary>
        [HttpGet]
        [Route("api/email-templates")]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var queryDTO = new PageQueryDTO
                {
                    Top = 100,
                    Skip = 0
                };

                var templates = await _templateService.GetPageQueryDataAsync(queryDTO);
                templates = templates.Where(t => t.UserID == userId).ToList();

                var result = templates.Select(t => new
                {
                    id = t.ID,
                    name = t.Name
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates for dropdown");
                return StatusCode(500, "Failed to load templates");
            }
        }

        /// <summary>
        /// API endpoint to get template details
        /// </summary>
        [HttpGet]
        [Route("api/email-templates/{id}")]
        public async Task<IActionResult> GetTemplateDetails(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var template = await _templateService.GetById(id);

                if (template == null || template.UserID != userId)
                {
                    return NotFound();
                }

                return Json(new
                {
                    subject = template.Subject,
                    body = template.Body,
                    placeholders = template.Placeholders
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template details for ID {TemplateId}", id);
                return StatusCode(500, "Failed to load template details");
            }
        }

        /// <summary>
        /// API endpoint to get email accounts
        /// </summary>
        [HttpGet]
        [Route("api/email-accounts")]
        public async Task<IActionResult> GetEmailAccounts()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var accounts = await _accountService.GetEmailAccountsOfUser(userId);
                var result = accounts.Select(email => new { email });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email accounts for dropdown");
                return StatusCode(500, "Failed to load email accounts");
            }
        }

        /// <summary>
        /// API endpoint to get recipient groups
        /// </summary>
        [HttpGet]
        [Route("api/groups")]
        public async Task<IActionResult> GetGroups()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var queryDTO = new PageQueryDTO
                {
                    Top = 100,
                    Skip = 0
                };

                var groups = await _groupService.GetPageQueryDataAsync(queryDTO);
                groups = groups.Where(g => g.UserID == userId).ToList();

                var result = groups.Select(g => new
                {
                    id = g.ID,
                    name = g.GroupName
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups for dropdown");
                return StatusCode(500, "Failed to load groups");
            }
        }

        #endregion
    }

    public class CreateJobRequest
    {
        public string TemplateId { get; set; }
        public string FromEmail { get; set; }
        public string GroupId { get; set; }
    }
}
