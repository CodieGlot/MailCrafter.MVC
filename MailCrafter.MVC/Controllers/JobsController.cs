using MailCrafter.Domain;
using MailCrafter.Services;
using MailCrafter.Utils.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class JobsController(
        IEmailTemplateService templateService,
        IAppUserService accountService,
        ICustomGroupService groupService,
        IEmailJobService jobService,
        MVCTaskQueueInstance taskQueue,
        IAesEncryptionHelper encryptionHelper,
        ILogger<JobsController> logger) : Controller
    {
        private readonly IEmailTemplateService _templateService = templateService;
        private readonly IAppUserService _accountService = accountService;
        private readonly ICustomGroupService _groupService = groupService;
        private readonly IEmailJobService _jobService = jobService;
        private readonly MVCTaskQueueInstance _taskQueue = taskQueue;
        private readonly IAesEncryptionHelper _encryptionHelper = encryptionHelper;
        private readonly ILogger<JobsController> _logger = logger;

        [Route("/jobs")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var jobs = await _jobService.GetJobsByUserId(userId);
            return View(jobs);
        }

        [Route("/jobs/details/{id}")]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetByIdAsync(id);

                if (job == null)
                {
                    return NotFound();
                }

                // Verify the job belongs to the user by checking FromEmail
                var userEmails = await _accountService.GetEmailAccountsOfUser(userId);
                if (!userEmails.Contains(job.FromEmail))
                {
                    return Forbid();
                }

                return View(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job details for ID {JobId}", id);
                return StatusCode(500, "Failed to load job details");
            }
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

                // Validate from email
                if (!IsValidEmail(request.FromEmail))
                {
                    return BadRequest("Invalid from email address format");
                }

                // Get email account details
                var emailAccounts = await _accountService.GetEmailAccountsOfUser(userId);
                var emailAccount = emailAccounts.FirstOrDefault(ea => ea == request.FromEmail);
                if (emailAccount == null)
                {
                    return BadRequest("Selected email account not found or not authorized");
                }

                // Get the app password for this email account
                var user = await _accountService.GetById(userId);
                var account = user.EmailAccounts.FirstOrDefault(ea => ea.Email == request.FromEmail);
                if (account == null || string.IsNullOrEmpty(account.AppPassword))
                {
                    return BadRequest("Email account is not properly configured. Please update the app password.");
                }

                // Handle recipients based on type (group or individual)
                var recipients = new List<string>();
                if (request.IsPersonalized && !string.IsNullOrEmpty(request.GroupId))
                {
                    // Group-based recipients
                    var group = await _groupService.GetById(request.GroupId);
                    if (group == null || group.UserID != userId)
                    {
                        return BadRequest("Invalid group selected");
                    }

                    // Extract and validate emails from CustomFieldsList
                    recipients = group.CustomFieldsList
                        .Where(field => field.ContainsKey("Email") && !string.IsNullOrWhiteSpace(field["Email"]))
                        .Select(field => field["Email"].Trim())
                        .ToList();

                    // Validate each email address individually
                    var invalidEmails = new List<string>();
                    foreach (var email in recipients)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidEmails.Add(email);
                        }
                    }

                    if (invalidEmails.Any())
                    {
                        return BadRequest($"Invalid email addresses found in group: {string.Join(", ", invalidEmails)}");
                    }

                    if (!recipients.Any())
                    {
                        return BadRequest("No valid email addresses found in the selected group");
                    }
                }
                else if (request.Recipients != null && request.Recipients.Any())
                {
                    // Individual recipients
                    recipients = request.Recipients.Where(email => !string.IsNullOrWhiteSpace(email)).ToList();

                    // Validate each email address individually
                    var invalidEmails = new List<string>();
                    foreach (var email in recipients)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidEmails.Add(email);
                        }
                    }

                    if (invalidEmails.Any())
                    {
                        return BadRequest($"Invalid email addresses found: {string.Join(", ", invalidEmails)}");
                    }
                }
                else
                {
                    return BadRequest("No recipients specified");
                }

                // Validate CC and BCC if provided
                if (request.CC != null)
                {
                    var invalidCC = new List<string>();
                    foreach (var email in request.CC)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidCC.Add(email);
                        }
                    }

                    if (invalidCC.Any())
                    {
                        return BadRequest($"Invalid CC email addresses: {string.Join(", ", invalidCC)}");
                    }
                }

                if (request.BCC != null)
                {
                    var invalidBCC = new List<string>();
                    foreach (var email in request.BCC)
                    {
                        if (!IsValidEmail(email))
                        {
                            invalidBCC.Add(email);
                        }
                    }

                    if (invalidBCC.Any())
                    {
                        return BadRequest($"Invalid BCC email addresses: {string.Join(", ", invalidBCC)}");
                    }
                }

                // Create email job entity
                var emailJob = new EmailJobEntity
                {
                    Name = request.Name,
                    TemplateId = request.TemplateId,
                    TemplateName = template.Name,
                    FromEmail = request.FromEmail,
                    Recipients = recipients,
                    IsPersonalized = request.IsPersonalized,
                    GroupId = request.GroupId,
                    CC = request.CC ?? new List<string>(),
                    BCC = request.BCC ?? new List<string>(),
                    CustomFields = request.CustomFields ?? new Dictionary<string, string>(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    ScheduledFor = request.ScheduledFor,
                    TotalRecipients = recipients.Count
                };

                // Save the job to database
                var job = await _jobService.CreateAsync(emailJob);

                if (request.IsPersonalized)
                {
                    // Create personalized email details for the worker
                    var personalizedDetails = new PersonalizedEmailDetailsModel
                    {
                        JobId = job.ID,
                        TemplateID = request.TemplateId,
                        FromMail = request.FromEmail,
                        AppPassword = account.AppPassword,
                        CC = request.CC,
                        Bcc = request.BCC,
                        GroupID = request.GroupId
                    };

                    // Queue the personalized email sending task
                    await _taskQueue.EnqueueAsync(WorkerTaskNames.Send_Personailized_Email, personalizedDetails);
                }
                else
                {
                    // Create basic email details for the worker
                    var details = new BasicEmailDetailsModel
                    {
                        JobId = job.ID,
                        TemplateID = request.TemplateId,
                        Recipients = recipients,
                        FromMail = request.FromEmail,
                        AppPassword = account.AppPassword,
                        CC = request.CC,
                        Bcc = request.BCC,
                        CustomFields = request.CustomFields
                    };

                    // Queue the basic email sending task
                    await _taskQueue.EnqueueAsync(WorkerTaskNames.Send_Basic_Email, details);
                }

                return Ok(new { message = "Email job created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email job");
                return StatusCode(500, "Failed to create email job");
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Email is null or whitespace");
                return false;
            }

            try
            {
                // More permissive email validation regex that accepts more valid email formats
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                var isValid = Regex.IsMatch(email, pattern);
                _logger.LogInformation("Validating email: {Email}, Result: {IsValid}", email, isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email: {Email}", email);
                return false;
            }
        }

        private bool AreValidEmails(List<string> emails)
        {
            if (emails == null || !emails.Any())
            {
                _logger.LogWarning("Email list is null or empty");
                return false;
            }

            var invalidEmails = emails.Where(email => !IsValidEmail(email)).ToList();
            if (invalidEmails.Any())
            {
                _logger.LogWarning("Invalid emails found: {InvalidEmails}", string.Join(", ", invalidEmails));
                return false;
            }

            return true;
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
                    Skip = 0,
                    Search = userId,
                    SearchBy = "UserID"
                };

                var templates = await _templateService.GetPageQueryDataAsync(queryDTO);
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

        /// <summary>
        /// API endpoint to delete a job
        /// </summary>
        [HttpDelete]
        [Route("api/jobs/{id}")]
        public async Task<IActionResult> DeleteJob(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetByIdAsync(id);

                if (job == null)
                {
                    return NotFound("Job not found");
                }

                // Verify the job belongs to the user by checking FromEmail
                var userEmails = await _accountService.GetEmailAccountsOfUser(userId);
                if (!userEmails.Contains(job.FromEmail))
                {
                    return Forbid("You don't have permission to delete this job");
                }

                // Don't allow deletion of processing jobs
                if (job.Status == "Processing")
                {
                    return BadRequest("Cannot delete a job that is currently processing");
                }

                await _jobService.DeleteAsync(id);
                _logger.LogInformation("Job {JobId} deleted successfully", id);

                return Ok(new { message = "Job deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job {JobId}", id);
                return StatusCode(500, "Failed to delete job");
            }
        }

        /// <summary>
        /// API endpoint to retry a failed job
        /// </summary>
        [HttpPost]
        [Route("api/jobs/{id}/retry")]
        public async Task<IActionResult> RetryJob(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var job = await _jobService.GetByIdAsync(id);

                if (job == null)
                {
                    return NotFound("Job not found");
                }

                // Verify the job belongs to the user by checking FromEmail
                var userEmails = await _accountService.GetEmailAccountsOfUser(userId);
                if (!userEmails.Contains(job.FromEmail))
                {
                    return Forbid("You don't have permission to retry this job");
                }

                // Only allow retrying failed or pending jobs
                if (job.Status != "Failed" && job.Status != "Pending")
                {
                    return BadRequest("Can only retry failed or pending jobs");
                }

                // Reset job status
                job.Status = "Pending";
                job.ErrorMessage = null;
                job.StartedAt = null;
                job.CompletedAt = null;
                job.ProcessedRecipients = 0;
                job.FailedRecipients = 0;

                await _jobService.UpdateAsync(job);

                // Get the app password for this email account
                var user = await _accountService.GetById(userId);
                var account = user.EmailAccounts.FirstOrDefault(ea => ea.Email == job.FromEmail);
                if (account == null || string.IsNullOrEmpty(account.AppPassword))
                {
                    return BadRequest("Email account is not properly configured. Please update the app password.");
                }

                // Create email details for the worker
                var details = new BasicEmailDetailsModel
                {
                    JobId = job.ID,
                    TemplateID = job.TemplateId,
                    Recipients = job.Recipients,
                    FromMail = job.FromEmail,
                    AppPassword = account.AppPassword,
                    CC = job.CC,
                    Bcc = job.BCC,
                    CustomFields = job.CustomFields
                };

                // Queue the email sending task
                await _taskQueue.EnqueueAsync(WorkerTaskNames.Send_Basic_Email, details);

                _logger.LogInformation("Job {JobId} queued for retry", id);
                return Ok(new { message = "Job queued for retry" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying job {JobId}", id);
                return StatusCode(500, "Failed to retry job");
            }
        }

        #endregion
    }

    public class CreateJobRequest
    {
        public string Name { get; set; }
        public string TemplateId { get; set; }
        public string FromEmail { get; set; }
        public string GroupId { get; set; }
        public List<string> Recipients { get; set; }
        public bool IsPersonalized { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }
        public DateTime? ScheduledFor { get; set; }
    }
}
