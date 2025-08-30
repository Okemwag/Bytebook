using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _basePath;
    private readonly string _baseUrl;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _basePath = _configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = _configuration["FileStorage:BaseUrl"] ?? "https://localhost:5001/files";

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<Result<string>> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null || fileStream.Length == 0)
            {
                return Result<string>.ValidationFailure("File", "File stream is empty or null");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result<string>.ValidationFailure("FileName", "File name cannot be empty");
            }

            // Sanitize file name
            var sanitizedFileName = SanitizeFileName(fileName);
            var fileExtension = Path.GetExtension(sanitizedFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
            
            // Create unique file name to avoid conflicts
            var uniqueFileName = $"{fileNameWithoutExtension}_{Guid.NewGuid()}{fileExtension}";
            
            // Create directory structure
            var directory = Path.GetDirectoryName(fileName) ?? "";
            var fullDirectory = Path.Combine(_basePath, directory);
            if (!Directory.Exists(fullDirectory))
            {
                Directory.CreateDirectory(fullDirectory);
            }

            // Full file path
            var filePath = Path.Combine(_basePath, directory, uniqueFileName);
            
            // Save file to disk
            using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);

            // Generate public URL
            var relativePath = Path.Combine(directory, uniqueFileName).Replace('\\', '/');
            var fileUrl = $"{_baseUrl}/{relativePath}";

            _logger.LogInformation("File uploaded successfully: {FileName} -> {FilePath}", fileName, filePath);

            return Result<string>.Success(fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return Result<string>.Failure("An error occurred while uploading the file");
        }
    }

    public async Task<Result<Stream>> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return Result<Stream>.ValidationFailure("FileUrl", "File URL cannot be empty");
            }

            // Extract relative path from URL
            var relativePath = ExtractRelativePathFromUrl(fileUrl);
            if (string.IsNullOrEmpty(relativePath))
            {
                return Result<Stream>.ValidationFailure("FileUrl", "Invalid file URL format");
            }

            var filePath = Path.Combine(_basePath, relativePath);
            
            if (!File.Exists(filePath))
            {
                return Result<Stream>.ValidationFailure("File", "File not found");
            }

            // Read file into memory stream to avoid file locking issues
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var memoryStream = new MemoryStream(fileBytes);

            _logger.LogDebug("File downloaded successfully: {FileUrl}", fileUrl);

            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FileUrl}", fileUrl);
            return Result<Stream>.Failure("An error occurred while downloading the file");
        }
    }

    public async Task<Result> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return Result.ValidationFailure("FileUrl", "File URL cannot be empty");
            }

            // Extract relative path from URL
            var relativePath = ExtractRelativePathFromUrl(fileUrl);
            if (string.IsNullOrEmpty(relativePath))
            {
                return Result.ValidationFailure("FileUrl", "Invalid file URL format");
            }

            var filePath = Path.Combine(_basePath, relativePath);
            
            if (!File.Exists(filePath))
            {
                // File doesn't exist, consider it already deleted
                _logger.LogWarning("Attempted to delete non-existent file: {FileUrl}", fileUrl);
                return Result.Success();
            }

            await Task.Run(() => File.Delete(filePath), cancellationToken);

            _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return Result.Failure("An error occurred while deleting the file");
        }
    }

    public async Task<Result<string>> GeneratePresignedUrlAsync(string fileUrl, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return Result<string>.ValidationFailure("FileUrl", "File URL cannot be empty");
            }

            // For local file storage, we'll generate a signed URL with expiry
            // In a real implementation with cloud storage (AWS S3, Azure Blob), this would use the provider's presigned URL feature
            
            var expiryTimestamp = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
            var signature = GenerateSignature(fileUrl, expiryTimestamp);
            
            var presignedUrl = $"{fileUrl}?expires={expiryTimestamp}&signature={signature}";

            await Task.CompletedTask; // Satisfy async requirement

            _logger.LogDebug("Presigned URL generated for: {FileUrl}, expires in {Expiry}", fileUrl, expiry);

            return Result<string>.Success(presignedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for: {FileUrl}", fileUrl);
            return Result<string>.Failure("An error occurred while generating presigned URL");
        }
    }

    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }

    private string? ExtractRelativePathFromUrl(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            
            // Remove leading /files/ from path
            if (path.StartsWith("/files/"))
            {
                return path[7..]; // Remove "/files/"
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateSignature(string fileUrl, long expiryTimestamp)
    {
        // Simple signature generation for demonstration
        // In production, use HMAC with a secret key
        var secretKey = _configuration["FileStorage:SecretKey"] ?? "default-secret-key";
        var data = $"{fileUrl}:{expiryTimestamp}";
        
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}