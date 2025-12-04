using ResendEmailApi.Models;

namespace ResendEmailApi.Services
{
    public interface IEmailService
    {
        Task<EmailResponse> SendEmailAsync(EmailRequest request);
        Task<BulkEmailResponse> SendBulkEmailAsync(BulkEmailRequest request);
        Task<EmailStatsResponse> GetEmailStatusAsync(string emailId);
        Task<EmailListResponse> GetRecentEmailsAsync(int limit = 10);
    }
}