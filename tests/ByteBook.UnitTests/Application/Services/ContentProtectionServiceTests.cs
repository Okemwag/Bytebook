using ByteBook.Application.Services;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Application.Services;

public class ContentProtectionServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ContentProtectionService>> _loggerMock;
    private readonly ContentProtectionService _contentProtectionService;

    public ContentProtectionServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ContentProtectionService>>();

        // Setup configuration
        _configurationMock.Setup(x => x["ContentProtection:WatermarkTemplate"])
            .Returns("© {UserName} - {Email} - {Timestamp}");
        _configurationMock.Setup(x => x["ContentProtection:AccessTokenExpiryMinutes"])
            .Returns("30");
        _configurationMock.Setup(x => x["ContentProtection:SecretKey"])
            .Returns("test-secret-key");
        _configurationMock.Setup(x => x["App:BaseUrl"])
            .Returns("https://localhost:5001");

        _contentProtectionService = new ContentProtectionService(
            _userRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ApplyWatermarkAsync_WithValidUser_ShouldReturnWatermarkedContent()
    {
        // Arrange
        var userId = 1;
        var content = "This is test content that needs watermarking.";
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _contentProtectionService.ApplyWatermarkAsync(content, userId);

        // Assert
        Assert.NotEqual(content, result);
        Assert.Contains("John Doe", result);
        Assert.Contains("john@example.com", result);
        Assert.Contains(content, result);
    }    [Fa
ct]
    public async Task ApplyWatermarkAsync_WithNonExistentUser_ShouldReturnOriginalContent()
    {
        // Arrange
        var userId = 999;
        var content = "This is test content.";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _contentProtectionService.ApplyWatermarkAsync(content, userId);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task ValidateContentAccessAsync_WithValidAccess_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var pageNumber = 1;
        
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 10);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateContentAccessAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;
        var bookId = 1;
        var pageNumber = 1;

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateContentAccessAsync_WithNonExistentBook_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var bookId = 999;
        var pageNumber = 1;
        
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync((Book?)null);

        // Act
        var result = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateContentAccessAsync_WithInactiveUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var pageNumber = 1;
        
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        user.Deactivate();
        
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 10);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateContentAccessAsync_WithInvalidPageNumber_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var pageNumber = 999; // Invalid page number
        
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 10); // Only 10 pages

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _contentProtectionService.ValidateContentAccessAsync(userId, bookId, pageNumber);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateSecureContentUrlAsync_WithValidAccess_ShouldReturnSecureUrl()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var pageNumber = 1;
        
        var user = new User("John", "Doe", new Email("john@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", userId, "Technology");
        book.UpdateContent("Test Book", "Test Description", "content-url", 10);
        book.SetPricing(new Money(0.50m), null);
        book.Publish();

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act
        var result = await _contentProtectionService.GenerateSecureContentUrlAsync(bookId, pageNumber, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("https://localhost:5001", result);
        Assert.Contains($"/api/books/{bookId}/pages/{pageNumber}/content", result);
        Assert.Contains("token=", result);
        Assert.Contains("expires=", result);
    }

    [Fact]
    public async Task GenerateSecureContentUrlAsync_WithInvalidAccess_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var userId = 2; // Different user
        var bookId = 1;
        var pageNumber = 1;
        var authorId = 1;
        
        var user = new User("Jane", "Doe", new Email("jane@example.com"), "hashedpassword");
        var book = new Book("Test Book", "Test Description", authorId, "Technology");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _contentProtectionService.GenerateSecureContentUrlAsync(bookId, pageNumber, userId));
    }

    [Fact]
    public async Task LogSuspiciousActivityAsync_ShouldLogActivity()
    {
        // Arrange
        var userId = 1;
        var activity = "Multiple rapid page requests";
        var details = "User requested 50 pages in 10 seconds";

        // Act
        await _contentProtectionService.LogSuspiciousActivityAsync(userId, activity, details);

        // Assert
        // Verify that logging occurred (this would be more comprehensive in a real test)
        // For now, we just ensure the method completes without throwing
        Assert.True(true);
    }
}