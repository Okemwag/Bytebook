using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Application.Services;

public class ContentProcessingServiceTests
{
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<ContentProcessingService>> _loggerMock;
    private readonly ContentProcessingService _contentProcessingService;

    public ContentProcessingServiceTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<ContentProcessingService>>();

        // Setup configuration
        _configurationMock.Setup(x => x["ContentProcessing:WordsPerPage"]).Returns("250");
        _configurationMock.Setup(x => x["ContentProcessing:PlagiarismThreshold"]).Returns("15.0");

        _contentProcessingService = new ContentProcessingService(
            _fileStorageServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessContentAsync_WithValidContent_ShouldReturnSuccess()
    {
        // Arrange
        var content = "This is a test content with multiple words to test the processing functionality. " +
                     "It should be formatted properly and calculate the correct number of pages based on word count.";

        _fileStorageServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<string>.Success("https://example.com/content.txt"));

        // Act
        var result = await _contentProcessingService.ProcessContentAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.TotalPages > 0);
        Assert.NotEmpty(result.Value.Content);
        Assert.Equal("https://example.com/content.txt", result.Value.ContentUrl);
        Assert.Contains("WordCount", result.Value.Metadata.Keys);

        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "text/plain", default), Times.Once);
    } 
   [Fact]
    public async Task ProcessContentAsync_WithEmptyContent_ShouldReturnValidationFailure()
    {
        // Arrange
        var content = "";

        // Act
        var result = await _contentProcessingService.ProcessContentAsync(content);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.ErrorMessage ?? "");

        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ProcessContentAsync_WithUploadFailure_ShouldReturnFailure()
    {
        // Arrange
        var content = "Test content";

        _fileStorageServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result<string>.Failure("Upload failed"));

        // Act
        var result = await _contentProcessingService.ProcessContentAsync(content);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Content upload failed", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ProcessPdfAsync_WithValidPdf_ShouldReturnSuccess()
    {
        // Arrange
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF header
        using var pdfStream = new MemoryStream(pdfContent);
        var fileName = "test.pdf";

        _fileStorageServiceMock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "application/pdf", default))
            .ReturnsAsync(ByteBook.Application.Common.Result<string>.Success("https://example.com/test.pdf"));

        // Act
        var result = await _contentProcessingService.ProcessPdfAsync(pdfStream, fileName);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.TotalPages > 0);
        Assert.NotEmpty(result.Value.Content);
        Assert.Equal("https://example.com/test.pdf", result.Value.ContentUrl);
        Assert.Contains("OriginalFileName", result.Value.Metadata.Keys);
        Assert.Contains("ContentType", result.Value.Metadata.Keys);
        Assert.Equal("pdf", result.Value.Metadata["ContentType"]);

        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "application/pdf", default), Times.Once);
    }

    [Fact]
    public async Task ProcessPdfAsync_WithEmptyStream_ShouldReturnValidationFailure()
    {
        // Arrange
        using var emptyStream = new MemoryStream();
        var fileName = "test.pdf";

        // Act
        var result = await _contentProcessingService.ProcessPdfAsync(emptyStream, fileName);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("empty or invalid", result.ErrorMessage ?? "");

        _fileStorageServiceMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task CheckPlagiarismAsync_WithCleanContent_ShouldReturnNotPlagiarized()
    {
        // Arrange
        var content = "This is original content that should not trigger plagiarism detection.";

        // Act
        var result = await _contentProcessingService.CheckPlagiarismAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsPlagiarized);
        Assert.True(result.Value.SimilarityPercentage < 15.0m);
    }

    [Fact]
    public async Task CheckPlagiarismAsync_WithSuspiciousContent_ShouldReturnPlagiarized()
    {
        // Arrange
        var content = "This content contains lorem ipsum and copy and paste text from wikipedia.";

        // Act
        var result = await _contentProcessingService.CheckPlagiarismAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsPlagiarized);
        Assert.True(result.Value.SimilarityPercentage >= 15.0m);
        Assert.NotEmpty(result.Value.Matches);
    }

    [Fact]
    public async Task CheckPlagiarismAsync_WithEmptyContent_ShouldReturnValidationFailure()
    {
        // Arrange
        var content = "";

        // Act
        var result = await _contentProcessingService.CheckPlagiarismAsync(content);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task FormatContentAsync_WithValidContent_ShouldReturnFormattedContent()
    {
        // Arrange
        var content = "This   is    poorly   formatted   content.\n\n\n\nWith   excessive   whitespace.";

        // Act
        var result = await _contentProcessingService.FormatContentAsync(content);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.DoesNotContain("   ", result.Value); // Should not have excessive spaces
        Assert.DoesNotContain("\n\n\n", result.Value); // Should not have excessive line breaks
    }

    [Fact]
    public async Task FormatContentAsync_WithEmptyContent_ShouldReturnValidationFailure()
    {
        // Arrange
        var content = "";

        // Act
        var result = await _contentProcessingService.FormatContentAsync(content);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("cannot be empty", result.ErrorMessage ?? "");
    }
}