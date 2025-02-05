namespace MailCrafter.Domain
{
    public class EmailAccount
    {
        public EmailAccountStatus Status { get; set; }
        public string Email { get; set; }
        public string Alias { get; set; }
        public string AppPassword { get; set; }
    }

    public enum EmailAccountStatus
    {
        NotVerified = 0,
        Active = 1,
        Suspended = 2,
        Revoked = 3,
        Deactivated = 4,
        Expired = 5
    }
}

