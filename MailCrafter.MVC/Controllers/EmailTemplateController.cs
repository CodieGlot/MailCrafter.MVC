using MailCrafter.Domain;
using MailCrafter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MailCrafter.MVC.Controllers
{
    [Authorize]
    public class EmailTemplateController : Controller
    {
        private readonly IEmailTemplateService _templateService;
        private readonly ILogger<EmailTemplateController> _logger;

        public EmailTemplateController(IEmailTemplateService templateService, ILogger<EmailTemplateController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        [HttpGet]
        [Route("management/email-templates")]
        public async Task<IActionResult> Index(PageQueryDTO queryDTO)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var templates = await _templateService.GetPageQueryDataAsync(queryDTO);
            templates = templates.Where(t => t.UserID == userId).ToList();
            return View(templates);
        }

        [HttpGet]
        [Route("templates/edit/{id?}")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                // Create new template
                return PartialView("_EmailTemplateEditor", new EmailTemplateEntity());
            }

            // Edit existing template
            var template = await _templateService.GetById(id);
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (template == null || template.UserID != userId)
            {
                return NotFound();
            }

            return PartialView("_EmailTemplateEditor", template);
        }

        [HttpPost]
        [Route("api/templates")]
        public async Task<IActionResult> SaveTemplate([FromBody] EmailTemplateEntity template)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                template.UserID = userId;

                if (string.IsNullOrEmpty(template.ID))
                {
                    // Create new template
                    template.CreatedAt = DateTime.UtcNow;
                    template.UpdatedAt = DateTime.UtcNow;
                    var result = await _templateService.Create(template);
                    return Ok(new { id = template.ID, success = result.IsSuccessful });
                }
                else
                {
                    // Update existing template
                    var existingTemplate = await _templateService.GetById(template.ID);
                    if (existingTemplate == null || existingTemplate.UserID != userId)
                    {
                        return NotFound();
                    }

                    template.CreatedAt = existingTemplate.CreatedAt;
                    template.UpdatedAt = DateTime.UtcNow;
                    var result = await _templateService.Update(template);
                    return Ok(new { id = template.ID, success = result.IsSuccessful });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving template");
                return StatusCode(500, "Failed to save template");
            }
        }

        [HttpPost]
        [Route("api/templates/upload")]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file selected");
            }

            try
            {
                string fileName = Path.GetFileName(file.FileName);
                string fileType = file.ContentType;

                // In a real application, you would upload to a storage service or save to disk
                // For simplicity, we'll just return file information
                string fileUrl = $"/uploads/{fileName}"; // Placeholder URL

                return Ok(new EmailFileInfo
                {
                    FileName = fileName,
                    FileType = fileType,
                    FileUrl = fileUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, "Failed to upload file");
            }
        }

        [HttpDelete]
        [Route("api/templates/{id}")]
        public async Task<IActionResult> DeleteTemplate(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Template ID is required");
            }

            try
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var template = await _templateService.GetById(id);

                if (template == null || template.UserID != userId)
                {
                    return NotFound();
                }

                var result = await _templateService.Delete(id);
                return Ok(new { success = result.IsSuccessful });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template with ID {TemplateId}", id);
                return StatusCode(500, "Failed to delete template");
            }
        }
    }
}
