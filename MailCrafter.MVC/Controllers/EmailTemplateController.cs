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

        [Route("/management/email-templates")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/management/email-templates/create")]
        public IActionResult CreateTemplate()
        {
            return View();
        }

    }
}
