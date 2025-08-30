using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ByteBook.Application.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _accountName;
    private readonly string _accountKey;
    private readonly string _containerName;
    private readonly string _cdnEndpoint;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _accountName = _configuration["Azure:Storage:AccountName"] ?? throw new InvalidOperationException("Azure Storage AccountName not configured");
        _accountKey = _configuration["Azure:Storage:AccountKey"] ?? throw new InvalidOperationException("Azure Storage AccountKey not configured");
        _containerName = _configuration["Azure:Storage:ContainerName"] ?? "bytebook-files";
        _cdnEndpoint = _configuration["Azure:CDN:Endpoint"] ?? "";
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

            // Sanitize and generate unique blob name
            var sanitizedFileName = SanitizeFileName(fileName);
            var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{sanitizedFileName}";

            // Prepare Azure Blob Storage request
            var url = $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}";
            var dateTime = DateTime.UtcNow;
            var dateString = dateTime.ToString("R"); // RFC 1123 format

            // Read file content
            fileStream.Position = 0;
            var fileContent = new byte[fileStream.Length];
            await fileStream.ReadAsync(fileContent, 0, fileContent.Length, cancellationToken);

            // Create Azure Storage authorization header
            var stringToSign = $"PUT\n\n\n{fileContent.Length}\n\n{contentType}\n\n\n\n\n\n\nx-ms-blob-type:BlockBlob\nx-ms-date:{dateString}\nx-ms-version:2020-04-08\n/{_accountName}/{_containerName}/{blobName}";
            var signature = ComputeHmacSha256(_accountKey, stringToSign);
            var authorizationHeader = $"SharedKey {_accountName}:{signature}";

            // Upload to Azure Blob Storage
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new ByteArrayContent(fileContent)
            };

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("x-ms-date", dateString);
            request.Headers.Add("x-ms-version", "2020-04-08");
            request.Headers.Add("x-ms-blob-type", "BlockBlob");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Azure Blob upload failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return Result<string>.Failure($"File upload failed: {response.StatusCode}");
            }

            // Generate public URL
            var publicUrl = string.IsNullOrEmpty(_cdnEndpoint)
                ? $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}"
                : $"https://{_cdnEndpoint}/{blobName}";

            _logger.LogInformation("File uploaded successfully to Azure Blob Storage: {FileName} -> {Url}", fileName, publicUrl);

            return Result<string>.Success(publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Azure Blob Storage: {FileName}", fileName);
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

            var response = await _httpClient.GetAsync(fileUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Result<Stream>.ValidationFailure("File", "File not found");
                }

                _logger.LogError("Azure Blob download failed: {StatusCode} for URL: {Url}", response.StatusCode, fileUrl);
                return Result<Stream>.Failure("Failed to download file");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogDebug("File downloaded successfully from Azure Blob Storage: {Url}", fileUrl);

            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from Azure Blob Storage: {Url}", fileUrl);
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

            // Extract blob name from URL
            var blobName = ExtractBlobNameFromUrl(fileUrl);
            if (string.IsNullOrEmpty(blobName))
            {
                return Result.ValidationFailure("FileUrl", "Invalid Azure Blob URL format");
            }

            var url = $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}";
            var dateTime = DateTime.UtcNow;
            var dateString = dateTime.ToString("R");

            var stringToSign = $"DELETE\n\n\n\n\n\n\n\n\n\n\n\nx-ms-date:{dateString}\nx-ms-version:2020-04-08\n/{_accountName}/{_containerName}/{blobName}";
            var signature = ComputeHmacSha256(_accountKey, stringToSign);
            var authorizationHeader = $"SharedKey {_accountName}:{signature}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("x-ms-date", dateString);
            request.Headers.Add("x-ms-version", "2020-04-08");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("File deleted successfully from Azure Blob Storage: {Url}", fileUrl);
                return Result.Success();
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Azure Blob delete failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
            return Result.Failure($"File deletion failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Azure Blob Storage: {Url}", fileUrl);
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

            var blobName = ExtractBlobNameFromUrl(fileUrl);
            if (string.IsNullOrEmpty(blobName))
            {
                return Result<string>.ValidationFailure("FileUrl", "Invalid Azure Blob URL format");
            }

            var startTime = DateTime.UtcNow.AddMinutes(-5); // Start 5 minutes ago to account for clock skew
            var expiryTime = DateTime.UtcNow.Add(expiry);

            var permissions = "r"; // Read permission
            var resourceType = "b"; // Blob
            var protocol = "https";

            var stringToSign = $"{permissions}\n{startTime:yyyy-MM-ddTHH:mm:ssZ}\n{expiryTime:yyyy-MM-ddTHH:mm:ssZ}\n/{_accountName}/{_containerName}/{blobName}\n\n{protocol}\n2020-04-08\n{resourceType}\n";
            var signature = ComputeHmacSha256(_accountKey, stringToSign);

            var sasToken = $"sv=2020-04-08&sr={resourceType}&sp={permissions}&st={startTime:yyyy-MM-ddTHH:mm:ssZ}&se={expiryTime:yyyy-MM-ddTHH:mm:ssZ}&spr={protocol}&sig={Uri.EscapeDataString(signature)}";
            var presignedUrl = $"https://{_accountName}.blob.core.windows.net/{_containerName}/{blobName}?{sasToken}";

            await Task.CompletedTask; // Satisfy async requirement

            _logger.LogDebug("Presigned URL generated for Azure Blob: {Url}, expires in {Expiry}", fileUrl, expiry);

            return Result<string>.Success(presignedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for Azure Blob: {Url}", fileUrl);
            return Result<string>.Failure("An error occurred while generating presigned URL");
        }
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { ' ' }).ToArray();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }

    private string? ExtractBlobNameFromUrl(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            
            // Handle CDN URLs
            if (!string.IsNullOrEmpty(_cdnEndpoint) && uri.Host.Contains(_cdnEndpoint))
            {
                return uri.AbsolutePath.TrimStart('/');
            }
            
            // Handle direct blob URLs
            if (uri.Host.Contains($"{_accountName}.blob.core.windows.net"))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                var segments = path.Split('/');
                if (segments.Length > 1 && segments[0] == _containerName)
                {
                    return string.Join("/", segments.Skip(1));
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string ComputeHmacSha256(string key, string data)
    {
        var keyBytes = Convert.FromBase64String(key);
        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}