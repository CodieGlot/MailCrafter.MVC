namespace MailCrafter.Domain
{
    public class AppUserEntity : MongoEntityBase
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<EmailAccount> EmailAccounts { get; set; } = new();
    }
}

