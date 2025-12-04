using Microsoft.AspNetCore.Mvc;
using Resend;
using ResendEmailApi.Models;
using ResendEmailApi.Services;

namespace ResendEmailApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Send a single email
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EmailResponse>> SendEmail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailService.SendEmailAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Send bulk emails to multiple recipients
        /// </summary>
        [HttpPost("send-bulk")]
        [ProducesResponseType(typeof(BulkEmailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkEmailResponse>> SendBulkEmail([FromBody] BulkEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.Recipients == null || !request.Recipients.Any())
            {
                return BadRequest("No recipients provided");
            }

            var result = await _emailService.SendBulkEmailAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get email status by message ID
        /// </summary>
        [HttpGet("status/{emailId}")]
        [ProducesResponseType(typeof(EmailStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmailStatsResponse>> GetEmailStatus(string emailId)
        {
            try
            {
                var status = await _emailService.GetEmailStatusAsync(emailId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for email {EmailId}", emailId);
                return NotFound(new { error = "Email not found or status unavailable", message = ex.Message });
            }
        }

        /// <summary>
        /// Get recent emails (default: last 10)
        /// </summary>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(EmailListResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<EmailListResponse>> GetRecentEmails([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1 || limit > 100)
                {
                    return BadRequest("Limit must be between 1 and 100");
                }

                var emails = await _emailService.GetRecentEmailsAsync(limit);
                return Ok(emails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent emails");
                return StatusCode(500, new { error = "Failed to retrieve emails", message = ex.Message });
            }
        }

        /// <summary>
        /// Get email statistics for a specific message ID (legacy endpoint)
        /// </summary>
        [HttpGet("stats/{emailId}")]
        [ProducesResponseType(typeof(EmailStatsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmailStatsResponse>> GetEmailStats(string emailId)
        {
            // Redirect to status endpoint
            return await GetEmailStatus(emailId);
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Resend Email API"
            });
        }

        /// <summary>
        /// Send a test email
        /// </summary>
        [HttpPost("test")]
        public async Task<ActionResult<EmailResponse>> SendTestEmail([FromBody] string toEmail)
        {
            var request = new EmailRequest
            {
                To = toEmail,
                Subject = "Test Email from Resend API",
                HtmlContent = @"
                    <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h1>Test Email</h1>
                            <p>This is a test email sent from the Resend Email API.</p>
                            <p>Timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"</p>
                        </body>
                    </html>",
                Tags = new List<EmailTag>
        {
            new EmailTag { Name = "category", Value = "test" },
            new EmailTag { Name = "environment", Value = "development" }
        }
            };

            var result = await _emailService.SendEmailAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}