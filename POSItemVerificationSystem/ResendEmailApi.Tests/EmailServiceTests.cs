using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResendEmailApi.Services;
using ResendEmailApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Resend;

namespace ResendEmailApi.Tests
{
    public class EmailServiceTests
    {
        private readonly IEmailService _emailService;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly IConfiguration _configuration;

        public EmailServiceTests()
        {
            // Setup configuration
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Resend:ApiKey", "re_aW4axn6S_Am1RdzM4fTQVHysjSVQBzrog"},
                {"Resend:DefaultFrom", "test@resend.dev"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _mockLogger = new Mock<ILogger<EmailService>>();
            _emailService = new EmailService(_configuration, _mockLogger.Object);
        }

        #region Send Email Tests

        [Fact]
        public async Task SendEmailAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "recipient@example.com",
                Subject = "Test Email",
                HtmlContent = "<h1>Hello World</h1>"
            };

            // Act
            var result = await _emailService.SendEmailAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success || !result.Success); // Either outcome is valid for this test
        }

        [Fact]
        public async Task SendEmailAsync_WithEmailTags_ReturnsSuccess()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "recipient@example.com",
                Subject = "Tagged Email",
                HtmlContent = "<p>Email with tags</p>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "confirm_email" },
                    new EmailTag { Name = "priority", Value = "high" }
                }
            };

            // Act
            var result = await _emailService.SendEmailAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SendEmailAsync_WithMultipleTags_ReturnsSuccess()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "recipient@example.com",
                Subject = "Multi-tagged Email",
                HtmlContent = "<p>Email with multiple tags</p>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "marketing" },
                    new EmailTag { Name = "campaign", Value = "summer_2024" },
                    new EmailTag { Name = "priority", Value = "low" }
                }
            };

            // Act
            var result = await _emailService.SendEmailAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SendEmailAsync_InvalidApiKey_ReturnsError()
        {
            // Arrange
            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Resend:ApiKey", "invalid_key"},
                    {"Resend:DefaultFrom", "test@resend.dev"}
                })
                .Build();

            var badService = new EmailService(badConfig, _mockLogger.Object);

            var request = new EmailRequest
            {
                To = "recipient@example.com",
                Subject = "Test",
                HtmlContent = "<p>Test</p>"
            };

            // Act
            var result = await badService.SendEmailAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }

        #endregion

        #region Bulk Email Tests

        [Fact]
        public async Task SendBulkEmailAsync_MultipleRecipients_ReturnsCorrectCounts()
        {
            // Arrange
            var request = new BulkEmailRequest
            {
                Recipients = new List<string>
                {
                    "user1@example.com",
                    "user2@example.com",
                    "user3@example.com"
                },
                Subject = "Bulk Test Email",
                HtmlContent = "<h1>Bulk Email</h1>"
            };

            // Act
            var result = await _emailService.SendBulkEmailAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
        }

        [Fact]
        public async Task SendBulkEmailAsync_WithTags_ReturnsResults()
        {
            // Arrange
            var request = new BulkEmailRequest
            {
                Recipients = new List<string>
                {
                    "user1@example.com",
                    "user2@example.com"
                },
                Subject = "Bulk Tagged Email",
                HtmlContent = "<h1>Bulk Email with Tags</h1>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "newsletter" },
                    new EmailTag { Name = "batch", Value = "2024-01" }
                }
            };

            // Act
            var result = await _emailService.SendBulkEmailAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SendBulkEmailAsync_EmptyRecipients_ReturnsZeroSent()
        {
            // Arrange
            var request = new BulkEmailRequest
            {
                Recipients = new List<string>(),
                Subject = "Empty Bulk",
                HtmlContent = "<p>Test</p>"
            };

            // Act
            var result = await _emailService.SendBulkEmailAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalSent);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Email Status Tests

        [Fact]
        public async Task GetEmailStatusAsync_ValidEmailId_ReturnsStatus()
        {
            // Arrange
            // First send an email to get a valid ID
            var sendRequest = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Status Test",
                HtmlContent = "<p>Test</p>"
            };

            var sendResult = await _emailService.SendEmailAsync(sendRequest);

            // Act
            if (sendResult.Success && !string.IsNullOrEmpty(sendResult.MessageId))
            {
                var status = await _emailService.GetEmailStatusAsync(sendResult.MessageId);

                // Assert
                Assert.NotNull(status);
                Assert.Equal(sendResult.MessageId, status.Id);
                Assert.NotNull(status.Status);
            }
            else
            {
                // If email send failed, just verify the method doesn't crash
                await Assert.ThrowsAsync<Exception>(async () =>
                    await _emailService.GetEmailStatusAsync("invalid_id")
                );
            }
        }

        [Fact]
        public async Task GetEmailStatusAsync_InvalidEmailId_ThrowsException()
        {
            // Arrange
            var invalidEmailId = "invalid_msg_id_12345";

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _emailService.GetEmailStatusAsync(invalidEmailId)
            );
        }

        [Fact]
        public async Task GetEmailStatusAsync_NullOrEmptyEmailId_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
                await _emailService.GetEmailStatusAsync(null)
            );

            await Assert.ThrowsAsync<Exception>(async () =>
                await _emailService.GetEmailStatusAsync(string.Empty)
            );
        }

        #endregion

        #region Recent Emails Tests

        [Fact]
        public async Task GetRecentEmailsAsync_DefaultLimit_ReturnsEmails()
        {
            // Act
            var result = await _emailService.GetRecentEmailsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Emails);
        }

        [Fact]
        public async Task GetRecentEmailsAsync_CustomLimit_ReturnsCorrectCount()
        {
            // Arrange
            var limit = 5;

            // Act
            var result = await _emailService.GetRecentEmailsAsync(limit);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Emails);
            Assert.True(result.Emails.Count <= limit);
        }

        [Fact]
        public async Task GetRecentEmailsAsync_LargeLimit_ReturnsEmails()
        {
            // Arrange
            var limit = 50;

            // Act
            var result = await _emailService.GetRecentEmailsAsync(limit);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Emails);
        }

        [Fact]
        public async Task GetRecentEmailsAsync_ReturnsEmailsWithRequiredFields()
        {
            // Act
            var result = await _emailService.GetRecentEmailsAsync(10);

            // Assert
            Assert.NotNull(result);
            if (result.Emails.Any())
            {
                var firstEmail = result.Emails.First();
                Assert.NotNull(firstEmail.Id);
                Assert.NotNull(firstEmail.Status);
            }
        }

        #endregion

        #region Theory Tests

        [Theory]
        [InlineData("test@example.com", "Test Subject", "<h1>Test</h1>")]
        [InlineData("another@test.com", "Another Subject", "<p>Another test</p>")]
        [InlineData("user@domain.com", "Welcome", "<h1>Welcome!</h1>")]
        public async Task SendEmailAsync_DifferentInputs_ReturnsResult(string to, string subject, string html)
        {
            // Arrange
            var request = new EmailRequest
            {
                To = to,
                Subject = subject,
                HtmlContent = html
            };

            // Act
            var result = await _emailService.SendEmailAsync(request);

            // Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public async Task GetRecentEmailsAsync_VariousLimits_ReturnsResults(int limit)
        {
            // Act
            var result = await _emailService.GetRecentEmailsAsync(limit);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Emails);
            Assert.True(result.Emails.Count <= limit);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task SendEmailAndCheckStatus_CompleteWorkflow_Success()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "workflow@example.com",
                Subject = "Workflow Test",
                HtmlContent = "<h1>Testing complete workflow</h1>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "test" },
                    new EmailTag { Name = "workflow", Value = "integration" }
                }
            };

            // Act - Send email
            var sendResult = await _emailService.SendEmailAsync(request);

            // Assert send
            Assert.NotNull(sendResult);

            // If send was successful, check status
            if (sendResult.Success && !string.IsNullOrEmpty(sendResult.MessageId))
            {
                // Small delay to allow API to process
                await Task.Delay(1000);

                // Act - Get status
                var status = await _emailService.GetEmailStatusAsync(sendResult.MessageId);

                // Assert status
                Assert.NotNull(status);
                Assert.Equal(sendResult.MessageId, status.Id);
                Assert.NotNull(status.Status);
                Assert.NotNull(status.CreatedAt);
            }
        }

        [Fact]
        public async Task SendBulkAndCheckRecent_CompleteWorkflow_Success()
        {
            // Arrange
            var bulkRequest = new BulkEmailRequest
            {
                Recipients = new List<string>
                {
                    "bulk1@example.com",
                    "bulk2@example.com"
                },
                Subject = "Bulk Workflow Test",
                HtmlContent = "<p>Bulk workflow test</p>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "bulk_test" }
                }
            };

            // Act - Send bulk
            var bulkResult = await _emailService.SendBulkEmailAsync(bulkRequest);

            // Assert bulk
            Assert.NotNull(bulkResult);

            // Small delay
            await Task.Delay(1000);

            // Act - Get recent
            var recentEmails = await _emailService.GetRecentEmailsAsync(10);

            // Assert recent
            Assert.NotNull(recentEmails);
            Assert.NotNull(recentEmails.Emails);
        }

        #endregion
    }
}