using RestSharp;
using Newtonsoft.Json;
using ResendEmailApi.Models;

namespace ResendEmailApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly ILogger<EmailService> _logger;
        private readonly string _defaultFrom;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _apiKey = configuration["Resend:ApiKey"] ?? throw new ArgumentNullException("Resend:ApiKey not configured");
            _logger = logger;
            _defaultFrom = configuration["Resend:DefaultFrom"] ?? "onboarding@resend.dev";
        }

        public async Task<EmailResponse> SendEmailAsync(EmailRequest request)
        {
            try
            {
                _logger.LogInformation("Sending email to {Recipient}", request.To);

                var client = new RestClient("https://api.resend.com");
                var restRequest = new RestRequest("/emails", Method.Post);

                restRequest.AddHeader("Authorization", $"Bearer {_apiKey}");
                restRequest.AddHeader("Content-Type", "application/json");

                var payload = new
                {
                    from = request.From ?? _defaultFrom,
                    to = new[] { request.To },
                    subject = request.Subject,
                    html = request.HtmlContent,
                    tags = request.Tags
                };

                restRequest.AddJsonBody(payload);

                var response = await client.ExecuteAsync(restRequest);

                if (!response.IsSuccessful)
                {
                    throw new Exception($"Resend API error: {response.Content}");
                }

                var result = JsonConvert.DeserializeObject<ResendApiResponse>(response.Content);

                _logger.LogInformation("Email sent successfully. Message ID: {MessageId}", result?.Id);

                return new EmailResponse
                {
                    Success = true,
                    MessageId = result?.Id,
                    Message = "Email sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", request.To);

                return new EmailResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<BulkEmailResponse> SendBulkEmailAsync(BulkEmailRequest request)
        {
            var response = new BulkEmailResponse();

            foreach (var recipient in request.Recipients)
            {
                try
                {
                    var emailRequest = new EmailRequest
                    {
                        To = recipient,
                        Subject = request.Subject,
                        HtmlContent = request.HtmlContent,
                        Tags = request.Tags
                    };

                    var result = await SendEmailAsync(emailRequest);

                    response.Results.Add(new EmailResult
                    {
                        Recipient = recipient,
                        Success = result.Success,
                        MessageId = result.MessageId,
                        Error = result.Error
                    });

                    if (result.Success)
                        response.TotalSent++;
                    else
                        response.TotalFailed++;

                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send bulk email to {Recipient}", recipient);

                    response.Results.Add(new EmailResult
                    {
                        Recipient = recipient,
                        Success = false,
                        Error = ex.Message
                    });
                    response.TotalFailed++;
                }
            }

            response.Success = response.TotalFailed == 0;
            return response;
        }

        // NEW METHOD: Get email status by ID
        public async Task<EmailStatsResponse> GetEmailStatusAsync(string emailId)
        {
            try
            {
                _logger.LogInformation("Getting email status for {EmailId}", emailId);

                var client = new RestClient("https://api.resend.com");
                var restRequest = new RestRequest($"/emails/{emailId}", Method.Get);

                restRequest.AddHeader("Authorization", $"Bearer {_apiKey}");

                var response = await client.ExecuteAsync(restRequest);

                if (!response.IsSuccessful)
                {
                    throw new Exception($"Failed to get email status: {response.Content}");
                }

                var email = JsonConvert.DeserializeObject<ResendEmailDetails>(response.Content);

                if (email == null)
                {
                    throw new Exception("Email not found");
                }

                return new EmailStatsResponse
                {
                    Id = email.Id,
                    Status = DetermineStatus(email),
                    CreatedAt = email.CreatedAt,
                    LastEvent = email.LastEvent,
                    From = email.From,
                    To = email.To?.FirstOrDefault(),
                    Subject = email.Subject
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get email status for {EmailId}", emailId);
                throw;
            }
        }

        // NEW METHOD: Get recent emails
        public async Task<EmailListResponse> GetRecentEmailsAsync(int limit = 10)
        {
            try
            {
                _logger.LogInformation("Getting recent emails (limit: {Limit})", limit);

                var client = new RestClient("https://api.resend.com");
                var restRequest = new RestRequest("/emails", Method.Get);

                restRequest.AddHeader("Authorization", $"Bearer {_apiKey}");
                restRequest.AddParameter("limit", limit);

                var response = await client.ExecuteAsync(restRequest);

                if (!response.IsSuccessful)
                {
                    throw new Exception($"Failed to get emails: {response.Content}");
                }

                var emailList = JsonConvert.DeserializeObject<ResendEmailListResponse>(response.Content);

                var emailStats = emailList?.Data?.Select(email => new EmailStatsResponse
                {
                    Id = email.Id,
                    Status = DetermineStatus(email),
                    CreatedAt = email.CreatedAt,
                    LastEvent = email.LastEvent,
                    From = email.From,
                    To = email.To?.FirstOrDefault(),
                    Subject = email.Subject
                }).ToList() ?? new List<EmailStatsResponse>();

                return new EmailListResponse
                {
                    Emails = emailStats,
                    TotalCount = emailStats.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent emails");
                throw;
            }
        }

        // Helper method to determine email status
        private string DetermineStatus(ResendEmailDetails email)
        {
            if (email.LastEvent == null)
                return "sent";

            // Status priority: bounced > delivered > sent
            // You can enhance this based on actual Resend status fields
            return "sent"; // Default status
        }

        // Helper classes for deserialization
        private class ResendApiResponse
        {
            public string Id { get; set; }
        }

        private class ResendEmailDetails
        {
            public string Id { get; set; }
            public string From { get; set; }
            public string[] To { get; set; }
            public string Subject { get; set; }
            public string Html { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? LastEvent { get; set; }
        }

        private class ResendEmailListResponse
        {
            public List<ResendEmailDetails> Data { get; set; }
        }
    }
}