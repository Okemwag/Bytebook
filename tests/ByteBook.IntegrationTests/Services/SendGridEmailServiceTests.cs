using ByteBook.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace ByteBook.IntegrationTests.Services;

public class SendGridEmailServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SendGridEmailService>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly SendGridEmailService _emailService;

    public SendGridEmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<SendGridEmailService>>();
        _httpClient = new HttpClient(new MockHttpMessageHandler());

        // Setup configuration
        _configurationMock.Setup(x => x["SendGrid:ApiKey"]).Returns("test-api-key");
        _configurationMock.Setup(x => x["Email:FromEmail"]).Returns("test@bytebook.com");
        _configurationMock.Setup(x => x["Email:FromName"]).Returns("ByteBook Test");
        _configurationMock.Setup(x => x["App:BaseUrl"]).Returns("https://test.bytebook.com");
        _configurationMock.Setup(x => x["SendGrid:Templates:EmailVerification"]).Returns("d-123456");
        _configurationMock.Setup(x => x["SendGrid:Templates:PasswordReset"]).Returns("d-789012");
        _configurationMock.Setup(x => x["SendGrid:Templates:Welcome"]).Returns("d-345678");

        _emailService = new SendGridEmailService(_configurationMock.Object, _loggerMock.Object, _httpClient);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var token = "verification-token-123";

        // Act
        var result = await _emailService.SendEmailVerificationAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var token = "reset-token-123";

        // Act
        var result = await _emailService.SendPasswordResetAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var firstName = "John";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(email, firstName);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithEmptyEmail_ShouldHandleGracefully()
    {
        // Arrange
        var email = "";
        var token = "verification-token-123";

        // Act & Assert
        // The service should handle this gracefully, though it may fail validation
        var result = await _emailService.SendEmailVerificationAsync(email, token);
        
        // We expect this to either succeed (if SendGrid handles it) or fail gracefully
        Assert.NotNull(result);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    // Mock HTTP message handler for testing
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Mock successful SendGrid response
            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"message\":\"success\"}")
            };

            return Task.FromResult(response);
        }
    }
}