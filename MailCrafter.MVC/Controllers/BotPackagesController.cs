using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MailCrafter.Domain.Common.MailCrafter.Domain.Common;
using MailCrafter.Domain.Common;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class BotPackagesController : Controller
    {
        private readonly ILogger<BotPackagesController> _logger;
        private readonly IBotPackageService _botPackageService;
        private readonly RobotService _robotService;
        private readonly WebSocketConnectionManager _websocketManager;
        private readonly IFileStorageService _fileStorageService;
        public BotPackagesController(
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

        // UI Routes
        [HttpGet]
        [Route("botpackages")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var queryDTO = new PageQueryDTO();
                var packages = await _botPackageService.GetPageQueryDataAsync(queryDTO);
                return View(packages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot packages for view");
                return View(new List<BotPackage>());
            }
        }

        [HttpGet]
        [Route("botpackages/details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound();
                }

                return View(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot package details for ID {PackageId}", id);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("botpackages/create")]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [Route("botpackages/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BotPackageCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.PackageFile == null || model.PackageFile.Length == 0)
                {
                    ModelState.AddModelError("PackageFile", "Please select a file to upload");
                    return View(model);
                }

                // Validate file extension
                string extension = Path.GetExtension(model.PackageFile.FileName).ToLowerInvariant();
                if (extension != ".zip" && extension != ".py")
                {
                    ModelState.AddModelError("PackageFile", "Only .zip and .py files are supported");
                    return View(model);
                }

                using (var stream = model.PackageFile.OpenReadStream())
                {
                    string fileName = $"{Guid.NewGuid()}{extension}";
                    string fileUrl = await _fileStorageService.UploadFileAsync(
                        stream,
                        fileName,
                        model.PackageFile.ContentType);

                    // Create package entity with the file URL from storage service
                    var package = new BotPackage(
                        model.Name,
                        model.Description ?? "", // Fix Problem 6: Ensure Description is not null
                        model.Version,
                        model.PackageFile.FileName,
                        model.PackageFile.Length,
                        model.PackageFile.ContentType)
                    {
                        FileUrl = fileUrl,
                        FileName = fileName
                    };

                    // Set uploader info - Fix Problem 8: Handle potentially null userId
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown User";
                    package.UploadedById = userId;
                    package.UploadedByName = userName;

                    // Save to database
                    var result = await _botPackageService.Create(package);
                    if (result == null || !result.IsSuccessful)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to save bot package");
                        return View(model);
                    }

                    return RedirectToAction(nameof(Details), new { id = package.ID });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bot package");
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        [Route("botpackages/deploy/{id}")]
        public async Task<IActionResult> Deploy(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound();
                }

                // Get all connected robots for selection
                var queryDTO = new PageQueryDTO();
                var robots = await _robotService.GetPageQueryDataAsync(queryDTO);

                var model = new DeployPackageViewModel
                {
                    Package = package,
                    AvailableRobots = robots.Where(r => r.IsConnected).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing to deploy package {PackageId}", id);
                return RedirectToAction(nameof(Index));
            }
        }
        // API Routes
        [HttpPost]
        [Route("api/botpackages/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPackage([FromForm] IFormFile file, [FromForm] string name,
            [FromForm] string? description, [FromForm] string? version)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name is required");
            }

            try
            {
                // Validate file extension
                string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".zip" && extension != ".py")
                {
                    return BadRequest("Only .zip and .py files are supported");
                }

                // Fix Problem 9: Instead of calling a non-existent method, implement the upload here:
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

                    // Set uploader info
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                    var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Unknown User";
                    package.UploadedById = userId;
                    package.UploadedByName = userName;

                    // Save to database
                    var result = await _botPackageService.Create(package);
                    if (result == null || !result.IsSuccessful)
                    {
                        return BadRequest("Failed to save bot package");
                    }

                    return Ok(package);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading bot package");
                return StatusCode(500, $"Error uploading package: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/botpackages/download/{fileName}")]
        public async Task<IActionResult> DownloadBotPackage(string fileName)
        {
            try
            {
                var (fileStream, contentType) = await _fileStorageService.GetFileAsync(fileName);

                if (fileStream == null)
                {
                    return NotFound("File not found");
                }

                return File(fileStream, contentType ?? "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading bot package file {FileName}", fileName);
                return StatusCode(500, "Error downloading file");
            }
        }




        [HttpPost]
        [Route("api/botpackages/deploy")]
        public async Task<IActionResult> DeployPackage([FromBody] DeployPackageRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PackageId) || string.IsNullOrEmpty(request.RobotId))
            {
                return BadRequest("Both packageId and robotId are required");
            }

            try
            {
                var package = await _botPackageService.GetById(request.PackageId);
                if (package == null)
                {
                    return NotFound("Package not found");
                }

                var robot = await _robotService.GetById(request.RobotId);
                if (robot == null)
                {
                    return NotFound("Robot not found");
                }

                if (!robot.IsConnected)
                {
                    return BadRequest("Cannot deploy to a disconnected robot");
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
                return StatusCode(500, $"Error deploying package: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("api/botpackages/list")]
        public async Task<IActionResult> GetPackages()
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
                return StatusCode(500, "Error retrieving packages");
            }
        }

        [HttpGet]
        [Route("api/botpackages/{id}")]
        public async Task<IActionResult> GetPackage(string id)
        {
            try
            {
                var package = await _botPackageService.GetById(id);
                if (package == null)
                {
                    return NotFound();
                }
                return Ok(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bot package {PackageId}", id);
                return StatusCode(500, "Error retrieving package");
            }
        }

        [HttpDelete]
        [Route("api/botpackages/{id}")]
        public async Task<IActionResult> DeletePackage(string id)
        {
            try
            {
                var result = await _botPackageService.Delete(id);
                if (!result.IsSuccessful)
                {
                    return NotFound();
                }
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bot package {PackageId}", id);
                return StatusCode(500, "Error deleting package");
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

    // View Models with fixes for Problems 1-7
    public class BotPackageCreateViewModel
    {
        public required string Name { get; set; } 
        public string? Description { get; set; } 
        public string Version { get; set; } = "1.0.0";
        public required IFormFile PackageFile { get; set; } 
    }

    public class DeployPackageViewModel
    {
        public required BotPackage Package { get; set; } 
        public required List<Robot> AvailableRobots { get; set; } 
    }

    public class DeployPackageRequest
    {
        public required string PackageId { get; set; } 
        public required string RobotId { get; set; } 
    }
}
