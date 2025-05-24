using MailCrafter.Domain;
using MailCrafter.Domain.Common;
using MailCrafter.Domain.Common.MailCrafter.Domain.Common;
using MailCrafter.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers.APIControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class APIBotPackageController : ControllerBase
    {
        private readonly ILogger<BotPackagesController> _logger;
        private readonly IBotPackageService _botPackageService;
        private readonly RobotService _robotService;
        private readonly WebSocketConnectionManager _websocketManager;
        private readonly IFileStorageService _fileStorageService;
        
        public APIBotPackageController(
            ILogger<BotPackagesController> logger,
            IBotPackageService botPackageService,
            RobotService robotService,
            WebSocketConnectionManager websocketManager,
            IFileStorageService fileStorageService)
        {
            _logger = logger;
            _botPackageService = botPackageService;
            _robotService = robotService;
            _websocketManager = websocketManager;
            _fileStorageService = fileStorageService;
        }

        // GET: api/APIBotPackage
        [HttpGet]
        public async Task<IActionResult> GetAllPackages()
        {
            try
            {
                var queryDTO = new PageQueryDTO();
                var packages = await _botPackageService.GetPageQueryDataAsync(queryDTO);
                return Ok(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot packages");
                return StatusCode(500, new { message = "Error retrieving packages", error = ex.Message });
            }
        }


        

        // GET: api/APIBotPackage/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackageById(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound(new { message = "Package not found" });
                }
                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot package {PackageId}", id);
                return StatusCode(500, new { message = "Error retrieving package", error = ex.Message });
            }
        }

        // POST: api/APIBotPackage/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPackage([FromForm] IFormFile file, [FromForm] string name,
            [FromForm] string? description, [FromForm] string? version)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is required" });
            }

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest(new { message = "Name is required" });
            }

            try
            {
                // Validate file extension
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".zip" && extension != ".py" && extension != ".nupkg")
                {
                    return BadRequest(new { message = "Only .zip, .py, and .nupkg files are supported" });
                }

                using (var stream = file.OpenReadStream())
                {
                    string fileName = $"{Guid.NewGuid()}{extension}";
                    string fileUrl = await _fileStorageService.UploadFileAsync(
                        stream,
                        fileName,
                        file.ContentType);

                    // Create package entity
                    var package = new BotPackage(
                        name,
                        description ?? "",
                        string.IsNullOrEmpty(version) ? "1.0.0" : version,
                        file.FileName,
                        file.Length,
                        file.ContentType)
                    {
                        FileUrl = fileUrl,
                        FileName = fileName
                    };

                    // Set uploader info if available
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown User";
                        package.UploadedById = userId;
                        package.UploadedByName = userName;
                    }

                    // Save to database
                    var result = await _botPackageService.Create(package);
                    if (result == null || !result.IsSuccessful)
                    {
                        return BadRequest(new { message = "Failed to save bot package" });
                    }

                    return Ok(new
                    {
                        success = true,
                        id = package.ID,
                        name = package.Name,
                        version = package.Version,
                        description = package.Description,
                        fileSize = package.FileSize,
                        uploadDate = package.UploadedAt,
                        message = "Package uploaded successfully"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading bot package");
                return StatusCode(500, new { message = "Error uploading package", error = ex.Message });
            }
        }

        // GET: api/APIBotPackage/download/{id}
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadPackage(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound(new { message = "Package not found" });
                }

                var (fileStream, contentType) = await _fileStorageService.GetFileAsync(package.FileName);

                if (fileStream == null)
                {
                    return NotFound(new { message = "File not found" });
                }

                // Use the original filename for better user experience
                return File(fileStream, contentType ?? "application/octet-stream", package.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading bot package {PackageId}", id);
                return StatusCode(500, new { message = "Error downloading package", error = ex.Message });
            }
        }

        // DELETE: api/APIBotPackage/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound(new { message = "Package not found" });
                }

                var result = await _botPackageService.Delete(id);
                if (!result.IsSuccessful)
                {
                    return StatusCode(500, new { message = "Failed to delete package" });
                }

                // Try to delete the file as well
                try
                {
                    await _fileStorageService.DeleteFileAsync(package.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deleting package file {FileName}", package.FileName);
                    // Continue even if file deletion fails
                }

                return Ok(new { success = true, message = "Package deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bot package {PackageId}", id);
                return StatusCode(500, new { message = "Error deleting package", error = ex.Message });
            }
        }

        // POST: api/APIBotPackage/deploy
        [HttpPost("deploy")]
        public async Task<IActionResult> DeployPackage([FromBody] DeployPackageRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PackageId) || string.IsNullOrEmpty(request.RobotId))
            {
                return BadRequest(new { message = "Both packageId and robotId are required" });
            }

            try
            {
                var package = await _botPackageService.GetById(request.PackageId);
                if (package == null)
                {
                    return NotFound(new { message = "Package not found" });
                }

                var robot = await _robotService.GetById(request.RobotId);
                if (robot == null)
                {
                    return NotFound(new { message = "Robot not found" });
                }

                if (!robot.IsConnected)
                {
                    return BadRequest(new { message = "Cannot deploy to a disconnected robot" });
                }

                // Create deployment command
                var command = new WebSocketCommandMessage(CommandTypes.BotDeployment)
                {
                    RobotId = robot.ID,
                    Payload = new
                    {
                        PackageId = package.ID,
                        PackageName = package.Name,
                        PackageVersion = package.Version,
                        FileUrl = GetAbsoluteUrl(package.FileUrl)
                    },
                    Timestamp = DateTime.UtcNow
                };

                // Send command to robot
                await _websocketManager.SendMessageAsync(robot.MachineKey, command);

                // Update package with deployment info
                if (!package.DeployedToRobotIds.Contains(robot.ID))
                {
                    package.DeployedToRobotIds.Add(robot.ID);
                    await _botPackageService.Update(package);
                }

                return Ok(new
                {
                    success = true,
                    message = $"Deployment command sent to robot {robot.MachineName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deploying package {PackageId} to robot {RobotId}",
                    request.PackageId, request.RobotId);
                return StatusCode(500, new { message = "Error deploying package", error = ex.Message });
            }
        }

        

        // Helper method to convert relative URL to absolute
        private string GetAbsoluteUrl(string relativeUrl)
        {
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";

            if (relativeUrl.StartsWith("http://") || relativeUrl.StartsWith("https://"))
            {
                return relativeUrl;
            }

            return $"{baseUrl}{relativeUrl}";
        }
    }

    public class DeployPackageRequest
    {
        public string PackageId { get; set; }
        public string RobotId { get; set; }
    }
}