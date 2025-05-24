using MailCrafter.Domain;
using MailCrafter.MVC.Models;
using MailCrafter.Services;
using MailCrafter.Services.Job;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class EmailScheduleController : Controller
    {
        private readonly IEmailScheduleService _scheduleService;
        private readonly IEmailTemplateService _templateService;
        private readonly IAppUserService _appUserService;
        private readonly ICustomGroupService _customGroupService;
        private readonly ILogger<EmailScheduleController> _logger;

        public EmailScheduleController(
            IEmailScheduleService scheduleService,
            IEmailTemplateService templateService,
            IAppUserService appUserService,
            ICustomGroupService customGroupService,
            ILogger<EmailScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _templateService = templateService;
            _appUserService = appUserService;
            _customGroupService = customGroupService;
            _logger = logger;
        }

        // GET: List all schedules
        [HttpGet]
        [Route("schedule")]
        [Route("EmailSchedule")]
        public async Task<IActionResult> Index()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var schedules = await _scheduleService.GetByUserID(userId);
                return View(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email schedules");
                TempData["ErrorMessage"] = "Failed to retrieve schedules. Please try again later.";
                return View(new List<EmailScheduleEntity>());
            }
        }

        // GET: Create a new schedule
        [HttpGet]
        [Route("schedule/create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get templates for dropdown
                var templates = await _templateService.GetByUserID(userId);
                ViewBag.Templates = new SelectList(templates, "ID", "Name");
                
                // Get groups for dropdown
                var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                groups = groups.Where(g => g.UserID == userId).ToList();
                ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                
                // Get email accounts for dropdown
                var user = await _appUserService.GetById(userId);
                ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                
                // Initialize the model with default values
                var model = new ScheduleEmailViewModel
                {
                    ScheduleDate = DateTime.Today.AddDays(1),
                    ScheduleTime = new TimeSpan(9, 0, 0), // 9:00 AM
                    Recurrence = RecurrencePattern.OneTime
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing schedule creation form");
                TempData["ErrorMessage"] = "Failed to load form. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Create a new schedule
        [HttpPost]
        [Route("schedule/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleEmailViewModel model)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (!ModelState.IsValid)
                {
                    // Repopulate dropdowns
                    var templates = await _templateService.GetByUserID(userId);
                    ViewBag.Templates = new SelectList(templates, "ID", "Name");
                    
                    var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                    var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                    groups = groups.Where(g => g.UserID == userId).ToList();
                    ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                    
                    var user = await _appUserService.GetById(userId);
                    ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                    
                    return View(model);
                }
                
                // Get template for validation and name
                var template = await _templateService.GetById(model.TemplateId);
                if (template == null || template.UserID != userId)
                {
                    ModelState.AddModelError("TemplateId", "Invalid template selection");
                    return View(model);
                }
                
                // Combine date and time for the schedule
                var scheduleDateTime = model.ScheduleDate.Date.Add(model.ScheduleTime);
                
                // Ensure schedule is in the future
                if (scheduleDateTime <= DateTime.Now)
                {
                    ModelState.AddModelError("ScheduleDate", "Schedule time must be in the future");
                    
                    // Repopulate dropdowns
                    var templates = await _templateService.GetByUserID(userId);
                    ViewBag.Templates = new SelectList(templates, "ID", "Name");
                    
                    var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                    var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                    groups = groups.Where(g => g.UserID == userId).ToList();
                    ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                    
                    var user = await _appUserService.GetById(userId);
                    ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                    
                    return View(model);
                }
                
                // Create email details based on whether it's personalized or not
                EmailDetailsAbstractModel emailDetails;
                
                if (model.IsPersonalized)
                {
                    // Validate group
                    var group = await _customGroupService.GetById(model.GroupId);
                    if (group == null || group.UserID != userId)
                    {
                        ModelState.AddModelError("GroupId", "Invalid group selection");
                        return View(model);
                    }
                    
                    // Personalized email (to a group)
                    emailDetails = new PersonalizedEmailDetailsModel
                    {
                        FromMail = model.FromEmail,
                        TemplateID = model.TemplateId,
                        AppPassword = GetAppPassword(userId, model.FromEmail),
                        GroupID = model.GroupId,
                        CC = model.CC ?? new List<string>(),
                        Bcc = model.BCC ?? new List<string>()
                    };
                }
                else
                {
                    // Regular email (to specific recipients)
                    if (model.Recipients == null || !model.Recipients.Any())
                    {
                        ModelState.AddModelError("Recipients", "At least one recipient is required");
                        return View(model);
                    }
                    
                    emailDetails = new BasicEmailDetailsModel
                    {
                        FromMail = model.FromEmail,
                        Recipients = model.Recipients,
                        TemplateID = model.TemplateId,
                        AppPassword = GetAppPassword(userId, model.FromEmail),
                        CC = model.CC ?? new List<string>(),
                        Bcc = model.BCC ?? new List<string>(),
                        CustomFields = model.CustomFields ?? new Dictionary<string, string>()
                    };
                }
                
                // Create the schedule entity
                var schedule = new EmailScheduleEntity
                {
                    UserID = userId,
                    Details = emailDetails,
                    NextSendTime = scheduleDateTime.ToUniversalTime(),
                    Recurrence = model.Recurrence
                };
                
                var result = await _scheduleService.Create(schedule);
                
                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Email schedule created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create schedule.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email schedule");
                TempData["ErrorMessage"] = "An error occurred while creating the schedule.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Edit a schedule
        [HttpGet]
        [Route("schedule/edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var schedule = await _scheduleService.GetById(id);
                if (schedule == null || schedule.UserID != userId)
                {
                    TempData["ErrorMessage"] = "Schedule not found or you do not have access.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Populate dropdowns
                var templates = await _templateService.GetByUserID(userId);
                ViewBag.Templates = new SelectList(templates, "ID", "Name");
                
                var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                groups = groups.Where(g => g.UserID == userId).ToList();
                ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                
                var user = await _appUserService.GetById(userId);
                ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                
                // Convert to view model
                var model = new ScheduleEmailViewModel
                {
                    ScheduleId = schedule.ID,
                    ScheduleDate = schedule.NextSendTime.ToLocalTime().Date,
                    ScheduleTime = schedule.NextSendTime.ToLocalTime().TimeOfDay,
                    Recurrence = schedule.Recurrence,
                    // Set other properties based on Details
                    FromEmail = GetFromEmail(schedule.Details),
                    TemplateId = GetTemplateId(schedule.Details)
                };
                
                // Set different properties based on the type of email
                if (schedule.Details is PersonalizedEmailDetailsModel personalizedDetails)
                {
                    model.IsPersonalized = true;
                    model.GroupId = personalizedDetails.GroupID;
                    model.CC = personalizedDetails.CC;
                    model.BCC = personalizedDetails.Bcc;
                }
                else if (schedule.Details is BasicEmailDetailsModel basicDetails)
                {
                    model.IsPersonalized = false;
                    model.Recipients = basicDetails.Recipients;
                    model.CC = basicDetails.CC;
                    model.BCC = basicDetails.Bcc;
                    model.CustomFields = basicDetails.CustomFields;
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedule for editing");
                TempData["ErrorMessage"] = "Failed to load schedule. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Update a schedule
        [HttpPost]
        [Route("schedule/edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ScheduleEmailViewModel model)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var schedule = await _scheduleService.GetById(id);
                if (schedule == null || schedule.UserID != userId)
                {
                    TempData["ErrorMessage"] = "Schedule not found or you do not have access.";
                    return RedirectToAction(nameof(Index));
                }
                
                if (!ModelState.IsValid)
                {
                    // Repopulate dropdowns
                    var templates = await _templateService.GetByUserID(userId);
                    ViewBag.Templates = new SelectList(templates, "ID", "Name");
                    
                    var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                    var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                    groups = groups.Where(g => g.UserID == userId).ToList();
                    ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                    
                    var user = await _appUserService.GetById(userId);
                    ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                    
                    return View(model);
                }
                
                // Combine date and time for the schedule
                var scheduleDateTime = model.ScheduleDate.Date.Add(model.ScheduleTime);
                
                // Ensure schedule is in the future
                if (scheduleDateTime <= DateTime.Now)
                {
                    ModelState.AddModelError("ScheduleDate", "Schedule time must be in the future");
                    
                    // Repopulate dropdowns
                    var templates = await _templateService.GetByUserID(userId);
                    ViewBag.Templates = new SelectList(templates, "ID", "Name");
                    
                    var groupQueryDTO = new PageQueryDTO { Top = 100, Skip = 0 };
                    var groups = await _customGroupService.GetPageQueryDataAsync(groupQueryDTO);
                    groups = groups.Where(g => g.UserID == userId).ToList();
                    ViewBag.Groups = new SelectList(groups, "ID", "GroupName");
                    
                    var user = await _appUserService.GetById(userId);
                    ViewBag.EmailAccounts = new SelectList(user.EmailAccounts, "Email", "Email");
                    
                    return View(model);
                }
                
                // Create email details based on whether it's personalized or not
                EmailDetailsAbstractModel emailDetails;
                
                if (model.IsPersonalized)
                {
                    // Validate group
                    var group = await _customGroupService.GetById(model.GroupId);
                    if (group == null || group.UserID != userId)
                    {
                        ModelState.AddModelError("GroupId", "Invalid group selection");
                        return View(model);
                    }
                    
                    // Personalized email (to a group)
                    emailDetails = new PersonalizedEmailDetailsModel
                    {
                        FromMail = model.FromEmail,
                        TemplateID = model.TemplateId,
                        AppPassword = GetAppPassword(userId, model.FromEmail),
                        GroupID = model.GroupId,
                        CC = model.CC ?? new List<string>(),
                        Bcc = model.BCC ?? new List<string>()
                    };
                }
                else
                {
                    // Regular email (to specific recipients)
                    if (model.Recipients == null || !model.Recipients.Any())
                    {
                        ModelState.AddModelError("Recipients", "At least one recipient is required");
                        return View(model);
                    }
                    
                    emailDetails = new BasicEmailDetailsModel
                    {
                        FromMail = model.FromEmail,
                        Recipients = model.Recipients,
                        TemplateID = model.TemplateId,
                        AppPassword = GetAppPassword(userId, model.FromEmail),
                        CC = model.CC ?? new List<string>(),
                        Bcc = model.BCC ?? new List<string>(),
                        CustomFields = model.CustomFields ?? new Dictionary<string, string>()
                    };
                }
                
                // Update the schedule entity
                schedule.Details = emailDetails;
                schedule.NextSendTime = scheduleDateTime.ToUniversalTime();
                schedule.Recurrence = model.Recurrence;
                
                var result = await _scheduleService.Update(schedule);
                
                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Email schedule updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update schedule.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email schedule");
                TempData["ErrorMessage"] = "An error occurred while updating the schedule.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Delete a schedule
        [HttpPost]
        [Route("schedule/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                var schedule = await _scheduleService.GetById(id);
                if (schedule == null || schedule.UserID != userId)
                {
                    TempData["ErrorMessage"] = "Schedule not found or you do not have access.";
                    return RedirectToAction(nameof(Index));
                }
                
                var result = await _scheduleService.Delete(id);
                
                if (result.IsSuccessful)
                {
                    TempData["SuccessMessage"] = "Email schedule deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete schedule.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email schedule");
                TempData["ErrorMessage"] = "An error occurred while deleting the schedule.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to get app password for a user's email account
        private string GetAppPassword(string userId, string email)
        {
            try
            {
                var user = _appUserService.GetById(userId).Result;
                if (user == null) return string.Empty;
                
                var emailAccount = user.EmailAccounts.FirstOrDefault(e => e.Email == email);
                return emailAccount?.AppPassword ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Helper method to get the FromEmail from the details
        private string GetFromEmail(EmailDetailsAbstractModel details)
        {
            if (details == null) return string.Empty;
            
            if (details is PersonalizedEmailDetailsModel personalizedDetails)
            {
                return personalizedDetails.FromMail;
            }
            else if (details is BasicEmailDetailsModel basicDetails)
            {
                return basicDetails.FromMail;
            }
            
            return string.Empty;
        }

        // Helper method to get the TemplateId from the details
        private string GetTemplateId(EmailDetailsAbstractModel details)
        {
            if (details == null) return string.Empty;
            
            if (details is PersonalizedEmailDetailsModel personalizedDetails)
            {
                return personalizedDetails.TemplateID;
            }
            else if (details is BasicEmailDetailsModel basicDetails)
            {
                return basicDetails.TemplateID;
            }
            
            return string.Empty;
        }

        // GET: Display monitoring dashboard for scheduled jobs
        [HttpGet]
        [Route("schedule/monitor")]
        public async Task<IActionResult> Monitor()
        {
            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get all schedules for the current user
                var schedules = await _scheduleService.GetByUserID(userId);
                
                // We only care about upcoming schedules now
                var upcomingSchedules = schedules.Where(s => s.NextSendTime > DateTime.UtcNow).OrderBy(s => s.NextSendTime).ToList();
                
                // For monitoring purposes, also get statistics
                var oneTimeCount = schedules.Count(s => s.Recurrence == RecurrencePattern.OneTime);
                var recurringCount = schedules.Count(s => s.Recurrence != RecurrencePattern.OneTime);
                
                ViewBag.UpcomingSchedules = upcomingSchedules;
                ViewBag.TotalCount = schedules.Count;
                ViewBag.OneTimeCount = oneTimeCount;
                ViewBag.RecurringCount = recurringCount;
                
                // Calculate next execution time based on actual schedule data
                if (upcomingSchedules.Any())
                {
                    // Get the closest upcoming schedule time
                    ViewBag.NextExecution = upcomingSchedules.First().NextSendTime.ToLocalTime();
                    ViewBag.HasScheduledRuns = true;
                }
                else
                {
                    // No upcoming schedules
                    ViewBag.NextExecution = DateTime.Now;
                    ViewBag.HasScheduledRuns = false;
                }
                
                // Also set the next Quartz job run time
                ViewBag.NextJobRun = DateTime.Now.AddMinutes(1);
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedule monitoring data");
                TempData["ErrorMessage"] = "Failed to load monitoring dashboard. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 