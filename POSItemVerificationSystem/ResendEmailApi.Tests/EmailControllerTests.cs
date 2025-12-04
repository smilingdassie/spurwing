using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ResendEmailApi.Controllers;
using ResendEmailApi.Services;
using ResendEmailApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Resend;

namespace ResendEmailApi.Tests
{
    public class EmailControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<EmailController>> _mockLogger;
        private readonly EmailController _controller;

        public EmailControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<EmailController>>();
            _controller = new EmailController(_mockEmailService.Object, _mockLogger.Object);
        }

        #region Send Email Tests

        [Fact]
        public async Task SendEmail_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Test",
                HtmlContent = "<h1>Test</h1>"
            };

            var expectedResponse = new EmailResponse
            {
                Success = true,
                MessageId = "msg_123",
                Message = "Email sent successfully"
            };

            _mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendEmail(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<EmailResponse>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("msg_123", response.MessageId);
        }

        [Fact]
        public async Task SendEmail_WithTags_ReturnsOk()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Tagged Email",
                HtmlContent = "<h1>Test</h1>",
                Tags = new List<EmailTag>
                {
                    new EmailTag { Name = "category", Value = "test" }
                }
            };

            var expectedResponse = new EmailResponse
            {
                Success = true,
                MessageId = "msg_456"
            };

            _mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendEmail(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<EmailResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task SendEmail_ServiceFails_ReturnsBadRequest()
        {
            // Arrange
            var request = new EmailRequest
            {
                To = "test@example.com",
                Subject = "Test",
                HtmlContent = "<h1>Test</h1>"
            };

            var expectedResponse = new EmailResponse
            {
                Success = false,
                Error = "API Error"
            };

            _mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendEmail(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<EmailResponse>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.NotNull(response.Error);
        }

        #endregion

        #region Get Email Status Tests

        [Fact]
        public async Task GetEmailStatus_ValidEmailId_ReturnsOk()
        {
            // Arrange
            var emailId = "msg_123";
            var expectedStats = new EmailStatsResponse
            {
                Id = emailId,
                Status = "sent",
                From = "sender@example.com",
                To = "recipient@example.com",
                Subject = "Test Email",
                CreatedAt = DateTime.UtcNow
            };

            _mockEmailService
                .Setup(x => x.GetEmailStatusAsync(emailId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetEmailStatus(emailId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsType<EmailStatsResponse>(okResult.Value);
            Assert.Equal(emailId, stats.Id);
            Assert.Equal("sent", stats.Status);
        }

        [Fact]
        public async Task GetEmailStatus_InvalidEmailId_ReturnsNotFound()
        {
            // Arrange
            var emailId = "invalid_id";

            _mockEmailService
                .Setup(x => x.GetEmailStatusAsync(emailId))
                .ThrowsAsync(new Exception("Email not found"));

            // Act
            var result = await _controller.GetEmailStatus(emailId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetEmailStatus_ServiceThrowsException_ReturnsNotFound()
        {
            // Arrange
            var emailId = "msg_error";

            _mockEmailService
                .Setup(x => x.GetEmailStatusAsync(emailId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetEmailStatus(emailId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.NotNull(notFoundResult.Value);
        }

        #endregion

        #region Get Recent Emails Tests

        [Fact]
        public async Task GetRecentEmails_DefaultLimit_ReturnsOk()
        {
            // Arrange
            var expectedEmails = new EmailListResponse
            {
                Emails = new List<EmailStatsResponse>
                {
                    new EmailStatsResponse { Id = "msg_1", Status = "sent" },
                    new EmailStatsResponse { Id = "msg_2", Status = "sent" }
                },
                TotalCount = 2
            };

            _mockEmailService
                .Setup(x => x.GetRecentEmailsAsync(10))
                .ReturnsAsync(expectedEmails);

            // Act
            var result = await _controller.GetRecentEmails();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<EmailListResponse>(okResult.Value);
            Assert.Equal(2, response.TotalCount);
            Assert.Equal(2, response.Emails.Count);
        }

        [Fact]
        public async Task GetRecentEmails_CustomLimit_ReturnsOk()
        {
            // Arrange
            var limit = 20;
            var expectedEmails = new EmailListResponse
            {
                Emails = new List<EmailStatsResponse>(),
                TotalCount = 0
            };

            _mockEmailService
                .Setup(x => x.GetRecentEmailsAsync(limit))
                .ReturnsAsync(expectedEmails);

            // Act
            var result = await _controller.GetRecentEmails(limit);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetRecentEmails_InvalidLimit_ReturnsBadRequest()
        {
            // Arrange
            var invalidLimit = 0;

            // Act
            var result = await _controller.GetRecentEmails(invalidLimit);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRecentEmails_LimitTooHigh_ReturnsBadRequest()
        {
            // Arrange
            var invalidLimit = 101;

            // Act
            var result = await _controller.GetRecentEmails(invalidLimit);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetRecentEmails_ServiceThrowsException_ReturnsServerError()
        {
            // Arrange
            _mockEmailService
                .Setup(x => x.GetRecentEmailsAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetRecentEmails();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion

        #region Bulk Email Tests

        [Fact]
        public async Task SendBulkEmail_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new BulkEmailRequest
            {
                Recipients = new List<string> { "test1@example.com", "test2@example.com" },
                Subject = "Bulk Test",
                HtmlContent = "<p>Bulk</p>"
            };

            var expectedResponse = new BulkEmailResponse
            {
                Success = true,
                TotalSent = 2,
                TotalFailed = 0
            };

            _mockEmailService
                .Setup(x => x.SendBulkEmailAsync(It.IsAny<BulkEmailRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendBulkEmail(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<BulkEmailResponse>(okResult.Value);
            Assert.Equal(2, response.TotalSent);
            Assert.Equal(0, response.TotalFailed);
        }

        [Fact]
        public async Task SendBulkEmail_NoRecipients_ReturnsBadRequest()
        {
            // Arrange
            var request = new BulkEmailRequest
            {
                Recipients = new List<string>(),
                Subject = "Test",
                HtmlContent = "<p>Test</p>"
            };

            // Act
            var result = await _controller.SendBulkEmail(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region Health and Test Email Tests

        [Fact]
        public void Health_ReturnsOk()
        {
            // Act
            var result = _controller.Health();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task SendTestEmail_ValidEmail_ReturnsOk()
        {
            // Arrange
            var testEmail = "test@example.com";

            var expectedResponse = new EmailResponse
            {
                Success = true,
                MessageId = "msg_test_123"
            };

            _mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<EmailRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendTestEmail(testEmail);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<EmailResponse>(okResult.Value);
            Assert.True(response.Success);
        }

        #endregion
    }
}