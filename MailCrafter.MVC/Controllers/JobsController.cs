using MailCrafter.Domain;
using MailCrafter.Services;
using MailCrafter.Services.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly IEmailJobService _jobService;
        private readonly IEmailTemplateService _templateService;
        private readonly IAppUserService _accountService;
        private readonly ICustomGroupService _groupService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            IEmailJobService jobService,
            IEmailTemplateService templateService,
            IAppUserService accountService,
            ICustomGroupService groupService,
            ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _templateService = templateService;
            _accountService = accountService;
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>
        /// Display the jobs list page
        /// </summary>
        [HttpGet]
        [Route("jobs")]
        public async Task<IActionResult> Index(PageQueryDTO queryDTO)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                //var queryDTO = new PageQueryDTO
                //{
                //    Top = 100,  // Get most recent jobs, can implement pagination later
                //    Skip = 0,
                //    SortBy = "CreatedAt",
                //    SortOrder = SortOrder.Desc
                //};
                 queryDTO = new PageQueryDTO
                {
                    Top = 100,
                    Skip = 0
                };

                var jobs = await _jobService.GetPageQueryDataAsync(queryDTO);
                jobs = jobs.Where(j => j.UserID == userId).ToList();

                return View(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs");
                TempData["ErrorMessage"] = "Failed to retrieve jobs. Please try again later.";
                return View(new List<EmailJobEntity>());
            }
        }

        /// <summary>
        /// Show detailed information about a specific job
        /// </summary>
        [HttpGet]
        [Route("jobs/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Job ID is required");
            }

            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetById(id);

                if (job == null || job.UserID != userId)
                {
                    return NotFound();
                }

                return View(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job details for ID {JobId}", id);
                TempData["ErrorMessage"] = "Failed to retrieve job details. Please try again later.";
                return RedirectToAction(nameof(Index));
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

                var result = templates.Select(t => new {
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

                var result = groups.Select(g => new {
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

        /// <summary>
        /// Create a new job
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("api/jobs")]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Get template to verify access and get name
                var template = await _templateService.GetById(request.TemplateId);
                if (template == null || template.UserID != userId)
                {
                    return BadRequest("Invalid template selection");
                }

                // Create the job entity
                var job = new EmailJobEntity
                {
                    UserID = userId,
                    Name = request.Name,
                    TemplateID = request.TemplateId,
                    TemplateName = template.Name,
                    FromEmail = request.FromEmail,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Set recipients based on type
                if (request.IsPersonalized)
                {
                    // Group-based
                    var group = await _groupService.GetById(request.GroupId);
                    if (group == null || group.UserID != userId)
                    {
                        return BadRequest("Invalid group selection");
                    }

                    job.GroupID = request.GroupId;
                    job.IsPersonalized = true;
                    job.Recipients = new List<string>();
                }
                else
                {
                    // Individual recipients
                    job.Recipients = request.Recipients;
                    job.CustomFields = request.CustomFields;
                    job.IsPersonalized = false;
                }

                // CC and BCC
                job.CC = request.Cc ?? new List<string>();
                job.Bcc = request.Bcc ?? new List<string>();

                // Save to database
                var result = await _jobService.Create(job);
                return Json(new { id = job.ID, success = result.IsSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job");
                return StatusCode(500, "Failed to create job");
            }
        }

        /// <summary>
        /// Delete a job
        /// </summary>
        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Route("api/jobs/{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetById(id);

                if (job == null || job.UserID != userId)
                {
                    return NotFound();
                }

                if (job.Status == "Processing")
                {
                    return BadRequest("Cannot delete a job that is currently processing");
                }

                var result = await _jobService.Delete(id);
                return Json(new { success = result.IsSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {JobId}", id);
                return StatusCode(500, "Failed to delete job");
            }
        }

        /// <summary>
        /// Retry a failed job
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("api/jobs/{id}/retry")]
        public async Task<IActionResult> RetryJob(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetById(id);

                if (job == null || job.UserID != userId)
                {
                    return NotFound();
                }

                if (job.Status != "Failed" && job.Status != "Pending")
                {
                    return BadRequest("Can only retry failed or pending jobs");
                }

                // Reset job status and error message
                job.Status = "Pending";
                job.ErrorMessage = string.Empty;
                job.UpdatedAt = DateTime.UtcNow;

                var result = await _jobService.Update(job);

                return Json(new { success = result.IsSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying job with ID {JobId}", id);
                return StatusCode(500, "Failed to retry job");
            }
        }

        #endregion
    }

    /// <summary>
    /// Request model for creating a new job
    /// </summary>
    public class CreateJobRequest
    {
        public string Name { get; set; }
        public string TemplateId { get; set; }
        public string FromEmail { get; set; }
        public List<string> Recipients { get; set; }
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public string? GroupId { get; set; }
        public bool IsPersonalized { get; set; }
    }
}
