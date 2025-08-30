using ByteBook.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using Xunit;

namespace ByteBook.IntegrationTests.Services;

public class FileStorageServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<FileStorageService>> _loggerMock;
    private readonly FileStorageService _fileStorageService;
    private readonly string _testDirectory;

    public FileStorageServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<FileStorageService>>();
        
        // Create a temporary directory for testing
        _testDirectory = Path.Combine(Path.GetTempPath(), "ByteBookTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Setup configuration
        _configurationMock.Setup(x => x["FileStorage:BasePath"]).Returns(_testDirectory);
        _configurationMock.Setup(x => x["FileStorage:BaseUrl"]).Returns("https://test.bytebook.com/files");
        _configurationMock.Setup(x => x["FileStorage:SecretKey"]).Returns("test-secret-key");

        _fileStorageService = new FileStorageService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_WithValidFile_ShouldReturnSuccess()
    {
        // Arrange
        var fileName = "test-document.txt";
        var contentType = "text/plain";
        var fileContent = "This is a test document content.";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _fileStorageService.UploadFileAsync(fileStream, fileName, contentType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Contains("test-document", result.Value);
        Assert.Contains("https://test.bytebook.com/files", result.Value);
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyStream_ShouldReturnValidationFailure()
    {
        // Arrange
        var fileName = "empty-file.txt";
        var contentType = "text/plain";
        using var emptyStream = new MemoryStream();

        // Act
        var result = await _fileStorageService.UploadFileAsync(emptyStream, fileName, contentType);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("empty", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task UploadFileAsync_WithEmptyFileName_ShouldReturnValidationFailure()
    {
        // Arrange
        var fileName = "";
        var contentType = "text/plain";
        var fileContent = "Test content";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _fileStorageService.UploadFileAsync(fileStream, fileName, contentType);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("empty", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task DownloadFileAsync_WithValidUrl_ShouldReturnFileStream()
    {
        // Arrange - First upload a file
        var fileName = "download-test.txt";
        var contentType = "text/plain";
        var originalContent = "This is content for download test.";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));

        var uploadResult = await _fileStorageService.UploadFileAsync(uploadStream, fileName, contentType);
        Assert.True(uploadResult.IsSuccess);

        // Act
        var downloadResult = await _fileStorageService.DownloadFileAsync(uploadResult.Value!);

        // Assert
        Assert.True(downloadResult.IsSuccess);
        Assert.NotNull(downloadResult.Value);

        using var reader = new StreamReader(downloadResult.Value);
        var downloadedContent = await reader.ReadToEndAsync();
        Assert.Equal(originalContent, downloadedContent);
    }

    [Fact]
    public async Task DownloadFileAsync_WithNonExistentFile_ShouldReturnValidationFailure()
    {
        // Arrange
        var nonExistentUrl = "https://test.bytebook.com/files/non-existent-file.txt";

        // Act
        var result = await _fileStorageService.DownloadFileAsync(nonExistentUrl);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task DeleteFileAsync_WithValidUrl_ShouldReturnSuccess()
    {
        // Arrange - First upload a file
        var fileName = "delete-test.txt";
        var contentType = "text/plain";
        var fileContent = "This file will be deleted.";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        var uploadResult = await _fileStorageService.UploadFileAsync(fileStream, fileName, contentType);
        Assert.True(uploadResult.IsSuccess);

        // Act
        var deleteResult = await _fileStorageService.DeleteFileAsync(uploadResult.Value!);

        // Assert
        Assert.True(deleteResult.IsSuccess);

        // Verify file is actually deleted
        var downloadResult = await _fileStorageService.DownloadFileAsync(uploadResult.Value!);
        Assert.True(downloadResult.IsFailure);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ShouldReturnSuccess()
    {
        // Arrange
        var nonExistentUrl = "https://test.bytebook.com/files/non-existent-file.txt";

        // Act
        var result = await _fileStorageService.DeleteFileAsync(nonExistentUrl);

        // Assert
        Assert.True(result.IsSuccess); // Should succeed even if file doesn't exist
    }

    [Fact]
    public async Task GeneratePresignedUrlAsync_WithValidUrl_ShouldReturnSignedUrl()
    {
        // Arrange - First upload a file
        var fileName = "presigned-test.txt";
        var contentType = "text/plain";
        var fileContent = "This file will have a presigned URL.";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        var uploadResult = await _fileStorageService.UploadFileAsync(fileStream, fileName, contentType);
        Assert.True(uploadResult.IsSuccess);

        var expiry = TimeSpan.FromHours(1);

        // Act
        var presignedResult = await _fileStorageService.GeneratePresignedUrlAsync(uploadResult.Value!, expiry);

        // Assert
        Assert.True(presignedResult.IsSuccess);
        Assert.NotNull(presignedResult.Value);
        Assert.Contains("expires=", presignedResult.Value);
        Assert.Contains("signature=", presignedResult.Value);
    }

    [Fact]
    public async Task UploadFileAsync_WithSpecialCharactersInFileName_ShouldSanitizeFileName()
    {
        // Arrange
        var fileName = "test file with spaces & special chars!@#.txt";
        var contentType = "text/plain";
        var fileContent = "Test content with special filename.";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _fileStorageService.UploadFileAsync(fileStream, fileName, contentType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Should not contain special characters in the URL
        Assert.DoesNotContain(" ", result.Value);
        Assert.DoesNotContain("&", result.Value);
        Assert.DoesNotContain("!", result.Value);
    }

    [Fact]
    public async Task UploadFileAsync_WithLongFileName_ShouldTruncateFileName()
    {
        // Arrange
        var longFileName = new string('a', 250) + ".txt"; // Very long filename
        var contentType = "text/plain";
        var fileContent = "Test content with long filename.";
        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

        // Act
        var result = await _fileStorageService.UploadFileAsync(fileStream, longFileName, contentType);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // The URL should not be excessively long
        var uri = new Uri(result.Value);
        var fileName = Path.GetFileName(uri.AbsolutePath);
        Assert.True(fileName.Length <= 250); // Should be truncated
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}