using Microsoft.AspNetCore.Mvc;

namespace MailCrafter.MVC.Controllers
{
    public class EmailTemplateController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public EmailTemplateController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [Route("/management/email-template")]
        public IActionResult Index()
        {
            return View();
        }

        string GetHtmlFileCode()
        {
            string fullpath = GetHtmlFilePath();
            if (!System.IO.File.Exists(fullpath))
                return "<b>No saved data yet</b>";
            return System.IO.File.ReadAllText(fullpath);
        }
        string GetHtmlFilePath()
        {
            string filename = "/usertyped_htmlcontent.html";
            string fullpath = Path.Combine(_env.WebRootPath, filename.TrimStart('/'));
            return fullpath;
        }

        public IActionResult AjaxLoadHandler()
        {
            return Content(GetHtmlFileCode(), "text/html");
        }

        [HttpPost]
        public IActionResult AjaxSaveHandler(string htmlcode)
        {
            string fullpath = GetHtmlFilePath();
            System.IO.File.WriteAllText(fullpath, htmlcode);
            return Content("OK");
        }
    }
}
