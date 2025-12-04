using Resend;
using System.ComponentModel.DataAnnotations;

namespace ResendEmailApi.Models
{
    public class EmailRequest
    {
        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string To { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "HTML content is required")]
        public string HtmlContent { get; set; }

        public string? From { get; set; } // Optional: defaults to configured sender

        public List<EmailTag>? Tags { get; set; } // Optional: for categorization

        public Dictionary<string, string>? Headers { get; set; } // Optional: custom headers
    }

    public class BulkEmailRequest
    {
        [Required]
        public List<string> Recipients { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string HtmlContent { get; set; }

        public List<EmailTag>? Tags { get; set; } // Optional: for categorization
    }
}