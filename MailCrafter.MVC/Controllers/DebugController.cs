using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailCrafter.MVC.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly IDebugLogService _debugLogService;

        public DebugController(IDebugLogService debugLogService)
        {
            _debugLogService = debugLogService;
        }

        [HttpGet("view-logs")]
        public IActionResult GetLogs()
        {
            var logs = _debugLogService.GetLogs();
            return Ok(logs);
        }
    }
} 