using MailCrafter.Domain;
using System.ComponentModel.DataAnnotations;

namespace MailCrafter.MVC.Models
{
    public class ScheduleEmailViewModel
    {
        // Schedule ID (for edit)
        public string? ScheduleId { get; set; }
        
        // Schedule date and time
        [Required(ErrorMessage = "Schedule date is required")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ScheduleDate { get; set; }
        
        [Required(ErrorMessage = "Schedule time is required")]
        [Display(Name = "Time")]
        [DataType(DataType.Time)]
        public TimeSpan ScheduleTime { get; set; }
        
        // Recurrence pattern
        [Required(ErrorMessage = "Recurrence pattern is required")]
        [Display(Name = "Repeat")]
        public RecurrencePattern Recurrence { get; set; }
        
        // Email template selection
        [Required(ErrorMessage = "Email template is required")]
        [Display(Name = "Email Template")]
        public string TemplateId { get; set; }
        
        // Sender email
        [Required(ErrorMessage = "Sender email is required")]
        [Display(Name = "Send From")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string FromEmail { get; set; }
        
        // Email type
        [Display(Name = "Send to Group")]
        public bool IsPersonalized { get; set; }
        
        // Group selection (for personalized emails)
        [Display(Name = "Recipient Group")]
        public string GroupId { get; set; }
        
        // Individual recipients (for regular emails)
        [Display(Name = "Recipients")]
        public List<string> Recipients { get; set; } = new List<string>();
        
        // CC and BCC
        [Display(Name = "CC (Optional)")]
        public List<string> CC { get; set; } = new List<string>();
        
        [Display(Name = "BCC (Optional)")]
        public List<string> BCC { get; set; } = new List<string>();
        
        // Custom fields (for regular emails)
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();
    }
} 