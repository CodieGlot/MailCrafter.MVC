using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class RobotManagementController : Controller
    {
        private readonly RobotService _robotService;
        private readonly ILogger<RobotManagementController> _logger;

        public RobotManagementController(
            RobotService robotService,
            ILogger<RobotManagementController> logger)
        {
            _robotService = robotService;
            _logger = logger;
        }

        [HttpGet]
        [Route("robots")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var queryDTO = new PageQueryDTO();
                

                var robots = await _robotService.GetPageQueryDataAsync(queryDTO);
                return View(robots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving robots for view");
                return View(new List<Robot>());
            }
        }

        [HttpGet]
        [Route("robots/details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var robot = await _robotService.GetById(id);
                if (robot == null)
                {
                    return NotFound();
                }

                return View(robot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving robot details for ID {RobotId}", id);
                return RedirectToAction(nameof(Index));
            }
        }

        // Add more UI-related actions as needed
    }
}
