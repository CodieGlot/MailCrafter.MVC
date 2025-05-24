using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;
using MailCrafter.Domain.Common;
using MailCrafter.Domain;
using MailCrafter.Domain.Common.MailCrafter.Domain.Common;


namespace MailCrafter.MVC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RobotsController : ControllerBase
    {
        private readonly ILogger<RobotsController> _logger;
        private readonly RobotService _robotService;
        private readonly WebSocketConnectionManager _websocketManager;

        public RobotsController(
            ILogger<RobotsController> logger,
            RobotService robotService,
            WebSocketConnectionManager websocketManager)
        {
            _logger = logger;
            _robotService = robotService;
            _websocketManager = websocketManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRobots()
        {
            PageQueryDTO queryDTO = new PageQueryDTO();
            var robots = await _robotService.GetPageQueryDataAsync(queryDTO);
            return Ok(robots);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRobot(string id)
        {
            var robot = await _robotService.GetById(id);
            if (robot == null)
            {
                return NotFound();
            }
            return Ok(robot);
        }

        [HttpPost("connect")]
        public async Task<IActionResult> ConnectRobot([FromBody] RobotConnectionModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MachineKey) || string.IsNullOrEmpty(model.MachineName))
            {
                return BadRequest("Invalid connection data");
            }

            try
            {
                var robot = await _robotService.ConnectRobotAsync(model);

                // Return success with robot ID
                return Ok(new RobotConnectionResponse
                {
                    RobotId = robot.ID,
                    Success = true,
                    Message = "Connected successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting robot: {MachineName}", model.MachineName);
                return StatusCode(500, "Error connecting robot: " + ex.Message);
            }
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MachineKey))
            {
                return BadRequest("Invalid heartbeat data");
            }

            try
            {
                var robot = await _robotService.GetByMachineKeyAsync(model.MachineKey);
                robot.IsConnected = true;
                await _robotService.Update(robot);



                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing heartbeat for machine key: {MachineKey}", model.MachineKey);
                return StatusCode(500, "Error processing heartbeat: " + ex.Message);
            }
        }

        [HttpPost("command/{id}")]
        public async Task<IActionResult> SendCommand(string id, [FromBody] WebSocketCommandMessage command)
        {
            if (command == null)
            {
                return BadRequest("Command cannot be null");
            }

            try
            {
                var robot = await _robotService.GetById(id);
                if (robot == null)
                {
                    return NotFound("Robot not found");
                }

                if (!robot.IsConnected)
                {
                    return BadRequest("Robot is not connected");
                }

                // Set the robotId in the command if not already set
                if (command.RobotId == null || command.RobotId == null)
                {
                    command.RobotId = id;
                }

                await _websocketManager.SendMessageAsync(robot.MachineKey, command);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to robot: {RobotId}", id);
                return StatusCode(500, "Error sending command: " + ex.Message);
            }
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastCommand([FromBody] WebSocketCommandMessage command)
        {
            if (command == null)
            {
                return BadRequest("Command cannot be null");
            }

            try
            {
                await _websocketManager.BroadcastMessageAsync(command);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting command");
                return StatusCode(500, "Error broadcasting command: " + ex.Message);
            }
        }

        [HttpGet("ws")]
        public async Task AcceptWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            // Get the machine key from the header
            if (!HttpContext.Request.Headers.TryGetValue("X-Machine-Key", out var machineKeyValues) ||
                string.IsNullOrEmpty(machineKeyValues))
            {
                HttpContext.Response.StatusCode = 401;
                await HttpContext.Response.WriteAsync("Machine key is required");
                return;
            }

            string machineKey = machineKeyValues.ToString();

            // Verify the machine key exists in our database
            var robotExists = await _robotService.ExistsByMachineKeyAsync(machineKey);
            if (!robotExists)
            {
                HttpContext.Response.StatusCode = 403;
                await HttpContext.Response.WriteAsync("Invalid machine key");
                return;
            }

            // Accept the WebSocket connection
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            _logger.LogInformation("WebSocket connection accepted for machine key: {MachineKey}", machineKey);

            // Add the connection to the manager
            _websocketManager.AddConnection(machineKey, webSocket);

            var robot = await _robotService.GetByMachineKeyAsync(machineKey);
            robot.IsConnected = true;
            // Update the robot status
            await _robotService.Update(robot);

            // Request a status update from the robot
            await _websocketManager.SendMessageAsync(machineKey,
                new WebSocketCommandMessage(CommandTypes.Status));

            // Handle the WebSocket connection until it's closed
            await _websocketManager.HandleWebSocketMessagesAsync(webSocket, machineKey,
                HttpContext.RequestAborted);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRobot([FromBody] CreateRobotRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MachineName))
            {
                return BadRequest("Machine name is required");
            }



            try
            {
                string machineKey = Guid.NewGuid().ToString();
                await _robotService.Create(new Robot(machineName: request.MachineName, machineKey: machineKey));

                // Return just the ID, name, and machine key
                return Ok(new
                {
                    MachineName = request.MachineName,
                    MachineKey = machineKey,
                    Message = "Robot created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating robot with name: {MachineName}", request.MachineName);
                return StatusCode(500, $"Error creating robot: {ex.Message}");
            }
        }
        [HttpPost("disconnect")]
        public async Task<IActionResult> DisconnectRobot([FromBody] DisconnectRobotModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MachineKey))
            {
                return BadRequest("Invalid disconnect data");
            }

            try
            {
                _logger.LogInformation("Robot disconnect request received for machine key: {MachineKey}", model.MachineKey);

                var robot = await _robotService.GetByMachineKeyAsync(model.MachineKey);
                robot.IsConnected = false;
                await _robotService.Update(robot);


                // Close the WebSocket connection if it exists
                await _websocketManager.RemoveConnectionAsync(model.MachineKey);

                return Ok(new
                {
                    success = true,
                    message = "Robot disconnected successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting robot with machine key: {MachineKey}", model.MachineKey);
                return StatusCode(500, "Error disconnecting robot: " + ex.Message);
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetRobotStatus([FromQuery] string machineKey)
        {
            if (string.IsNullOrEmpty(machineKey))
            {
                return BadRequest("Machine key is required");
            }

            try
            {
                var robot = await _robotService.GetByMachineKeyAsync(machineKey);
                if (robot == null)
                {
                    return NotFound("Robot not found");
                }

                return Ok(new
                {
                    IsConnected = robot.IsConnected,
                    LastSeen = robot.LastSeen,
                    MachineName = robot.MachineName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status for machine key: {MachineKey}", machineKey);
                return StatusCode(500, "Error checking robot status: " + ex.Message);
            }
        }






    }

}
