using MailCrafter.Domain;

namespace MailCrafter.MVC.Models.AppUser
{
    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
