using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace MailCrafter.MVC.Controllers.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class APIRobotController : ControllerBase
    {
        private readonly RobotService _robotService;
        private readonly ILogger<APIRobotController> _logger;

        public APIRobotController(
            RobotService robotService,
            ILogger<APIRobotController> logger)
        {
            _robotService = robotService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRobots()
        {
            try
            {
                var queryDTO = new PageQueryDTO();
                var robots = await _robotService.GetPageQueryDataAsync(queryDTO);
                return Ok(robots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving robots");
                return StatusCode(500, new { message = "Error retrieving robots", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRobotById(string id)
        {
            try
            {
                var robot = await _robotService.GetById(id);
                if (robot == null)
                {
                    return NotFound(new { message = "Robot not found" });
                }

                return Ok(robot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving robot details for ID {RobotId}", id);
                return StatusCode(500, new { message = "Error retrieving robot details", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRobot([FromBody] CreateRobotRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MachineName))
                {
                    return BadRequest(new { message = "Machine name is required" });
                }

                string machineKey = Guid.NewGuid().ToString();
                var robot = new Robot(machineName: request.MachineName, machineKey: machineKey);
                
                await _robotService.Create(robot);

                return Ok(new
                {
                    robotId = robot.ID,
                    machineName = robot.MachineName,
                    machineKey = robot.MachineKey,
                    message = "Robot created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating robot with name: {MachineName}", request.MachineName);
                return StatusCode(500, new { message = "Error creating robot", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRobot(string id, [FromBody] UpdateRobotRequest request)
        {
            try
            {
                var robot = await _robotService.GetById(id);
                if (robot == null)
                {
                    return NotFound(new { message = "Robot not found" });
                }
                
                if (!string.IsNullOrEmpty(request.MachineName))
                {
                    robot.MachineName = request.MachineName;
                }

                var result = await _robotService.Update(robot);
                if (!result.IsSuccessful)
                {
                    return StatusCode(500, new { message = "Failed to update robot" });
                }

                return Ok(new { message = "Robot updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating robot with ID {RobotId}", id);
                return StatusCode(500, new { message = "Error updating robot", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRobot(string id)
        {
            try
            {
                var robot = await _robotService.GetById(id);
                if (robot == null)
                {
                    return NotFound(new { message = "Robot not found" });
                }

                var result = await _robotService.Delete(id);
                if (!result.IsSuccessful)
                {
                    return StatusCode(500, new { message = "Failed to delete robot" });
                }

                return Ok(new { message = "Robot deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting robot with ID {RobotId}", id);
                return StatusCode(500, new { message = "Error deleting robot", error = ex.Message });
            }
        }
    }

    public class CreateRobotRequest
    {
        public string MachineName { get; set; }
    }

    public class UpdateRobotRequest
    {
        public string? MachineName { get; set; }
    }
}