using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace MailCrafter.MVC.Models
{
    public class SendEmailViewModel
    {
        [Required(ErrorMessage = "Template is required")]
        [DisplayName("Email Template")]
        public string TemplateId { get; set; }
        
        [Required(ErrorMessage = "Sender email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [DisplayName("Send From")]
        public string FromEmail { get; set; }
        
        public string GroupId { get; set; }
        
        [DisplayName("CC (Optional)")]
        public List<string> CC { get; set; } = new List<string>();
        
        [DisplayName("BCC (Optional)")]
        public List<string> Bcc { get; set; } = new List<string>();
    }
} 