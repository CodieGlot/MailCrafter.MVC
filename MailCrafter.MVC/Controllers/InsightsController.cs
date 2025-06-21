using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class InsightsController : Controller
    {
        private readonly IEmailJobService _emailJobService;
        private readonly ILogger<InsightsController> _logger;

        public InsightsController(IEmailJobService emailJobService, ILogger<InsightsController> logger)
        {
            _emailJobService = emailJobService;
            _logger = logger;
        }

        [HttpGet]
        [Route("insights")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("api/insights/overview")]
        public async Task<IActionResult> GetOverview()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var jobs = await _emailJobService.GetJobsByUserId(userId);

                var totalEmailsSent = jobs.Sum(j => j.TotalRecipients);
                var processedEmails = jobs.Sum(j => j.ProcessedRecipients);
                var failedEmails = jobs.Sum(j => j.FailedRecipients);
                var pendingEmails = jobs.Where(j => j.Status != "Completed")
                    .Sum(j => j.TotalRecipients - j.ProcessedRecipients - j.FailedRecipients);

                var successRate = totalEmailsSent > 0 ? (processedEmails * 100.0 / totalEmailsSent) : 0;
                var failureRate = totalEmailsSent > 0 ? (failedEmails * 100.0 / totalEmailsSent) : 0;
                var pendingRate = totalEmailsSent > 0 ? (pendingEmails * 100.0 / totalEmailsSent) : 0;

                var totalOpened = jobs.Sum(j => j.OpenedEmails);
                var totalClicked = jobs.Sum(j => j.ClickedEmails);
                var averageOpenRate = totalEmailsSent > 0 ? (totalOpened * 100.0 / totalEmailsSent) : 0;
                var averageClickRate = totalEmailsSent > 0 ? (totalClicked * 100.0 / totalEmailsSent) : 0;

                return Json(new
                {
                    totalEmailsSent,
                    successRate = Math.Round(successRate, 1),
                    failureRate = Math.Round(failureRate, 1),
                    pendingRate = Math.Round(pendingRate, 1),
                    averageOpenRate = Math.Round(averageOpenRate, 1),
                    averageClickRate = Math.Round(averageClickRate, 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insights overview");
                return StatusCode(500, "Failed to get insights data");
            }
        }

        [HttpGet]
        [Route("api/insights/activity")]
        public async Task<IActionResult> GetActivity([FromQuery] string timeRange = "daily")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var jobs = await _emailJobService.GetJobsByUserId(userId);

                var (startDate, endDate, interval) = GetDateRange(timeRange);
                var dates = GenerateDateLabels(startDate, endDate, interval);

                var activityData = new
                {
                    labels = dates.Select(d => FormatDateLabel(d, timeRange)).ToList(),
                    sent = dates.Select(d => GetDataForDateRange(jobs, d, interval, j => j.TotalRecipients)).ToList(),
                    processed = dates.Select(d => GetDataForDateRange(jobs, d, interval, j => j.ProcessedRecipients)).ToList(),
                    failed = dates.Select(d => GetDataForDateRange(jobs, d, interval, j => j.FailedRecipients)).ToList(),
                    pending = dates.Select(d => {
                        var dateJobs = GetJobsForDateRange(jobs, d, interval);
                        return dateJobs.Sum(j => j.TotalRecipients - j.ProcessedRecipients - j.FailedRecipients);
                    }).ToList(),
                    opened = dates.Select(d => GetDataForDateRange(jobs, d, interval, j => j.OpenedEmails)).ToList(),
                    clicked = dates.Select(d => GetDataForDateRange(jobs, d, interval, j => j.ClickedEmails)).ToList()
                };

                return Json(activityData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity data");
                return StatusCode(500, "Failed to get activity data");
            }
        }

        private (DateTime startDate, DateTime endDate, TimeSpan interval) GetDateRange(string timeRange)
        {
            var endDate = DateTime.Now.Date;
            var startDate = timeRange switch
            {
                "weekly" => endDate.AddDays(-7),
                "monthly" => endDate.AddMonths(-1),
                "yearly" => endDate.AddYears(-1),
                _ => endDate.AddDays(-7) // Default to daily view of last 7 days
            };
            var interval = timeRange switch
            {
                "weekly" => TimeSpan.FromDays(1),
                "monthly" => TimeSpan.FromDays(7),
                "yearly" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(1)
            };
            return (startDate, endDate, interval);
        }

        private IEnumerable<DateTime> GenerateDateLabels(DateTime startDate, DateTime endDate, TimeSpan interval)
        {
            var dates = new List<DateTime>();
            for (var date = startDate; date <= endDate; date = date.Add(interval))
            {
                dates.Add(date);
            }
            return dates;
        }

        private string FormatDateLabel(DateTime date, string timeRange)
        {
            return timeRange switch
            {
                "weekly" => date.ToString("MMM dd"),
                "monthly" => $"Week {GetWeekNumber(date)}",
                "yearly" => date.ToString("MMM yyyy"),
                _ => date.ToString("MMM dd")
            };
        }

        private int GetWeekNumber(DateTime date)
        {
            return (date.Day - 1) / 7 + 1;
        }

        private IEnumerable<EmailJobEntity> GetJobsForDateRange(IEnumerable<EmailJobEntity> jobs, DateTime date, TimeSpan interval)
        {
            return jobs.Where(j => j.CreatedAt.Date >= date && j.CreatedAt.Date < date.Add(interval));
        }

        private int GetDataForDateRange(IEnumerable<EmailJobEntity> jobs, DateTime date, TimeSpan interval, Func<EmailJobEntity, int> selector)
        {
            return GetJobsForDateRange(jobs, date, interval).Sum(selector);
        }

        [HttpGet]
        [Route("api/insights/status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var jobs = await _emailJobService.GetJobsByUserId(userId);

                var totalProcessed = jobs.Sum(j => j.ProcessedRecipients);
                var totalFailed = jobs.Sum(j => j.FailedRecipients);
                var totalSent = jobs.Where(j => j.Status == "Completed")
                    .Sum(j => j.TotalRecipients - j.ProcessedRecipients - j.FailedRecipients);
                var totalPending = jobs.Where(j => j.Status != "Completed")
                    .Sum(j => j.TotalRecipients - j.ProcessedRecipients - j.FailedRecipients);

                var statusData = new
                {
                    labels = new[] { "Processed", "Failed", "Sent", "Pending" },
                    values = new[] { totalProcessed, totalFailed, totalSent, totalPending }
                };

                return Json(statusData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status data");
                return StatusCode(500, "Failed to get status data");
            }
        }

        [HttpGet]
        [Route("api/insights/top-templates")]
        public async Task<IActionResult> GetTopTemplates()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var jobs = await _emailJobService.GetJobsByUserId(userId);

                var templateStats = jobs
                    .GroupBy(j => j.TemplateId)
                    .Select(g => new
                    {
                        name = g.First().TemplateName,
                        sent = g.Sum(j => j.TotalRecipients),
                        processed = g.Sum(j => j.ProcessedRecipients),
                        failed = g.Sum(j => j.FailedRecipients)
                    })
                    .OrderByDescending(t => t.sent)
                    .Take(5)
                    .ToList();

                return Json(templateStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top templates data");
                return StatusCode(500, "Failed to get top templates data");
            }
        }

        [HttpGet]
        [Route("api/insights/account-performance")]
        public async Task<IActionResult> GetAccountPerformance()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var jobs = await _emailJobService.GetJobsByUserId(userId);

                var accountStats = jobs
                    .GroupBy(j => j.FromEmail)
                    .Select(g => new
                    {
                        email = g.Key,
                        sent = g.Sum(j => j.TotalRecipients),
                        processed = g.Sum(j => j.ProcessedRecipients),
                        failed = g.Sum(j => j.FailedRecipients)
                    })
                    .OrderByDescending(a => a.sent)
                    .ToList();

                return Json(accountStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account performance data");
                return StatusCode(500, "Failed to get account performance data");
            }
        }
    }
} 