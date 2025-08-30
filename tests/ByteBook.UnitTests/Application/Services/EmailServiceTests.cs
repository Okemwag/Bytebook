using ByteBook.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteBook.UnitTests.Application.Services;

public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        // Setup configuration
        _configurationMock.Setup(x => x["App:BaseUrl"]).Returns("https://bytebook.com");
        _configurationMock.Setup(x => x["Email:FromEmail"]).Returns("noreply@bytebook.com");
        _configurationMock.Setup(x => x["Email:FromName"]).Returns("ByteBook Platform");

        _emailService = new EmailService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "verification-token-123";

        // Act
        var result = await _emailService.SendEmailVerificationAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithSpecialCharactersInEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john+test@example.com";
        var token = "verification-token-123";

        // Act
        var result = await _emailService.SendEmailVerificationAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "reset-token-123";

        // Act
        var result = await _emailService.SendPasswordResetAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WithSpecialCharactersInEmail_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john+test@example.com";
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
        var email = "john.doe@example.com";
        var firstName = "John";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(email, firstName);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithSpecialCharactersInName_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var firstName = "José";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(email, firstName);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "verification-token-123";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _emailService.SendEmailVerificationAsync(email, token, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SendPasswordResetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "reset-token-123";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _emailService.SendPasswordResetAsync(email, token, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var email = "john.doe@example.com";
        var firstName = "John";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _emailService.SendWelcomeEmailAsync(email, firstName, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SendEmailVerificationAsync_WithEmptyToken_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "";

        // Act
        var result = await _emailService.SendEmailVerificationAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WithEmptyToken_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var token = "";

        // Act
        var result = await _emailService.SendPasswordResetAsync(email, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithEmptyFirstName_ShouldReturnSuccess()
    {
        // Arrange
        var email = "john.doe@example.com";
        var firstName = "";

        // Act
        var result = await _emailService.SendWelcomeEmailAsync(email, firstName);

        // Assert
        Assert.True(result.IsSuccess);
    }
}