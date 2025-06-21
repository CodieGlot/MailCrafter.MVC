using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MailCrafter.MVC.Controllers
{
    public class TrackingController : Controller
    {
        private readonly IEmailTrackingService _trackingService;
        private readonly ILogger<TrackingController> _logger;

        public TrackingController(IEmailTrackingService trackingService, ILogger<TrackingController> logger)
        {
            _trackingService = trackingService;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/tracking/test")]
        public IActionResult TestTracking()
        {
            _logger.LogInformation("=== TRACKING TEST ENDPOINT HIT ===");
            return Json(new { message = "Tracking endpoint is accessible", timestamp = DateTime.UtcNow });
        }

        [HttpGet]
        [Route("api/tracking/pixel/{data}")]
        public async Task<IActionResult> TrackOpen(string data)
        {
            try
            {
                _logger.LogInformation("=== TRACKING PIXEL REQUEST RECEIVED ===");
                _logger.LogInformation("Request URL: {RequestUrl}", Request.GetDisplayUrl());
                _logger.LogInformation("User Agent: {UserAgent}", Request.Headers.UserAgent.ToString());
                _logger.LogInformation("Referer: {Referer}", Request.Headers.Referer.ToString());
                _logger.LogInformation("Received tracking pixel request with data: {Data}", data);
                
                var (jobId, recipientEmail) = DecryptTrackingData(data);
                _logger.LogInformation("Decoded tracking data - JobId: {JobId}, Email: {Email}", jobId, recipientEmail);
                
                await _trackingService.TrackEmailOpen(jobId, recipientEmail);
                _logger.LogInformation("Successfully tracked email open for job {JobId}", jobId);

                // Return a 1x1 transparent GIF
                return File(Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"), "image/gif");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tracking pixel for data: {Data}", data);
                return StatusCode(500);
            }
        }

        [HttpGet]
        [Route("api/tracking/click/{data}")]
        public async Task<IActionResult> TrackClick(string data, [FromQuery] string url)
        {
            try
            {
                _logger.LogInformation("Received tracking click request with data: {Data}, url: {Url}", data, url);
                var (jobId, recipientEmail) = DecryptTrackingData(data);
                _logger.LogInformation("Decoded tracking data - JobId: {JobId}, Email: {Email}", jobId, recipientEmail);
                
                await _trackingService.TrackEmailClick(jobId, recipientEmail);
                _logger.LogInformation("Successfully tracked email click for job {JobId}", jobId);

                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest("URL parameter is required");
                }

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tracking click for data: {Data}, url: {Url}", data, url);
                return StatusCode(500);
            }
        }

        private (string jobId, string recipientEmail) DecryptTrackingData(string data)
        {
            // In a production environment, use proper decryption
            // This is a simple example using Base64 decoding
            var decodedData = Encoding.UTF8.GetString(Convert.FromBase64String(data));
            var parts = decodedData.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid tracking data format");
            }
            return (parts[0], parts[1]);
        }
    }
} 