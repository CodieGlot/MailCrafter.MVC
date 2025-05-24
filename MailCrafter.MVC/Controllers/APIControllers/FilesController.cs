using MailCrafter.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailCrafter.MVC.Controllers
{
    public class FilesController : Controller
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/files/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var (fileStream, contentType) = await _fileStorageService.GetFileAsync(fileName);

                if (fileStream == null)
                {
                    return NotFound("File not found");
                }

                // Stream the file directly to the client
                return File(fileStream, contentType ?? "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileName}", fileName);
                return StatusCode(500, "Error retrieving file");
            }
        }
    }
}
