using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ByteBook.Application.Services;

public class AwsS3FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsS3FileStorageService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _bucketName;
    private readonly string _region;
    private readonly string _accessKeyId;
    private readonly string _secretAccessKey;
    private readonly string _cloudFrontDomain;

    public AwsS3FileStorageService(
        IConfiguration configuration,
        ILogger<AwsS3FileStorageService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _bucketName = _configuration["AWS:S3:BucketName"] ?? throw new InvalidOperationException("AWS S3 BucketName not configured");
        _region = _configuration["AWS:S3:Region"] ?? "us-east-1";
        _accessKeyId = _configuration["AWS:AccessKeyId"] ?? throw new InvalidOperationException("AWS AccessKeyId not configured");
        _secretAccessKey = _configuration["AWS:SecretAccessKey"] ?? throw new InvalidOperationException("AWS SecretAccessKey not configured");
        _cloudFrontDomain = _configuration["AWS:CloudFront:Domain"] ?? "";
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

            // Sanitize and generate unique file name
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{sanitizedFileName}";

            // Prepare AWS S3 request
            var url = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{uniqueFileName}";
            var dateTime = DateTime.UtcNow;
            var dateStamp = dateTime.ToString("yyyyMMdd");
            var amzDate = dateTime.ToString("yyyyMMddTHHmmssZ");

            // Read file content
            fileStream.Position = 0;
            var fileContent = new byte[fileStream.Length];
            await fileStream.ReadAsync(fileContent, 0, fileContent.Length, cancellationToken);

            // Create AWS Signature V4
            var headers = new Dictionary<string, string>
            {
                { "host", $"{_bucketName}.s3.{_region}.amazonaws.com" },
                { "x-amz-date", amzDate },
                { "x-amz-content-sha256", ComputeSha256Hash(fileContent) },
                { "content-type", contentType }
            };

            var signature = CreateAwsSignature("PUT", uniqueFileName, headers, fileContent, dateStamp, amzDate);
            var authorizationHeader = $"AWS4-HMAC-SHA256 Credential={_accessKeyId}/{dateStamp}/{_region}/s3/aws4_request, SignedHeaders=content-type;host;x-amz-content-sha256;x-amz-date, Signature={signature}";

            // Upload to S3
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new ByteArrayContent(fileContent)
            };

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("x-amz-date", amzDate);
            request.Headers.Add("x-amz-content-sha256", ComputeSha256Hash(fileContent));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("S3 upload failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return Result<string>.Failure($"File upload failed: {response.StatusCode}");
            }

            // Generate public URL
            var publicUrl = string.IsNullOrEmpty(_cloudFrontDomain) 
                ? $"https://{_bucketName}.s3.{_region}.amazonaws.com/{uniqueFileName}"
                : $"https://{_cloudFrontDomain}/{uniqueFileName}";

            _logger.LogInformation("File uploaded successfully to S3: {FileName} -> {Url}", fileName, publicUrl);

            return Result<string>.Success(publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", fileName);
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

                _logger.LogError("S3 download failed: {StatusCode} for URL: {Url}", response.StatusCode, fileUrl);
                return Result<Stream>.Failure("Failed to download file");
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            _logger.LogDebug("File downloaded successfully from S3: {Url}", fileUrl);

            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file from S3: {Url}", fileUrl);
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

            // Extract S3 key from URL
            var s3Key = ExtractS3KeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(s3Key))
            {
                return Result.ValidationFailure("FileUrl", "Invalid S3 URL format");
            }

            var url = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{s3Key}";
            var dateTime = DateTime.UtcNow;
            var dateStamp = dateTime.ToString("yyyyMMdd");
            var amzDate = dateTime.ToString("yyyyMMddTHHmmssZ");

            var headers = new Dictionary<string, string>
            {
                { "host", $"{_bucketName}.s3.{_region}.amazonaws.com" },
                { "x-amz-date", amzDate },
                { "x-amz-content-sha256", ComputeSha256Hash(Array.Empty<byte>()) }
            };

            var signature = CreateAwsSignature("DELETE", s3Key, headers, Array.Empty<byte>(), dateStamp, amzDate);
            var authorizationHeader = $"AWS4-HMAC-SHA256 Credential={_accessKeyId}/{dateStamp}/{_region}/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature={signature}";

            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", authorizationHeader);
            request.Headers.Add("x-amz-date", amzDate);
            request.Headers.Add("x-amz-content-sha256", ComputeSha256Hash(Array.Empty<byte>()));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("File deleted successfully from S3: {Url}", fileUrl);
                return Result.Success();
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("S3 delete failed: {StatusCode} - {Content}", response.StatusCode, errorContent);
            return Result.Failure($"File deletion failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {Url}", fileUrl);
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

            var s3Key = ExtractS3KeyFromUrl(fileUrl);
            if (string.IsNullOrEmpty(s3Key))
            {
                return Result<string>.ValidationFailure("FileUrl", "Invalid S3 URL format");
            }

            var expirySeconds = (int)expiry.TotalSeconds;
            var dateTime = DateTime.UtcNow;
            var dateStamp = dateTime.ToString("yyyyMMdd");
            var amzDate = dateTime.ToString("yyyyMMddTHHmmssZ");

            var credentialScope = $"{dateStamp}/{_region}/s3/aws4_request";
            var credential = $"{_accessKeyId}/{credentialScope}";

            var queryParams = new Dictionary<string, string>
            {
                { "X-Amz-Algorithm", "AWS4-HMAC-SHA256" },
                { "X-Amz-Credential", credential },
                { "X-Amz-Date", amzDate },
                { "X-Amz-Expires", expirySeconds.ToString() },
                { "X-Amz-SignedHeaders", "host" }
            };

            var canonicalQueryString = string.Join("&", queryParams.OrderBy(kvp => kvp.Key).Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            
            var canonicalRequest = $"GET\n/{s3Key}\n{canonicalQueryString}\nhost:{_bucketName}.s3.{_region}.amazonaws.com\n\nhost\nUNSIGNED-PAYLOAD";
            var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";

            var signature = ComputeHmacSha256(
                ComputeHmacSha256(
                    ComputeHmacSha256(
                        ComputeHmacSha256(
                            ComputeHmacSha256(Encoding.UTF8.GetBytes($"AWS4{_secretAccessKey}"), dateStamp),
                            _region),
                        "s3"),
                    "aws4_request"),
                stringToSign);

            var presignedUrl = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{s3Key}?{canonicalQueryString}&X-Amz-Signature={Convert.ToHexString(signature).ToLower()}";

            await Task.CompletedTask; // Satisfy async requirement

            _logger.LogDebug("Presigned URL generated for S3 file: {Url}, expires in {Expiry}", fileUrl, expiry);

            return Result<string>.Success(presignedUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for S3 file: {Url}", fileUrl);
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

    private string? ExtractS3KeyFromUrl(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            
            // Handle CloudFront URLs
            if (!string.IsNullOrEmpty(_cloudFrontDomain) && uri.Host.Contains(_cloudFrontDomain))
            {
                return uri.AbsolutePath.TrimStart('/');
            }
            
            // Handle direct S3 URLs
            if (uri.Host.Contains($"{_bucketName}.s3") || uri.Host.Contains("s3.amazonaws.com"))
            {
                return uri.AbsolutePath.TrimStart('/');
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string CreateAwsSignature(string method, string s3Key, Dictionary<string, string> headers, byte[] payload, string dateStamp, string amzDate)
    {
        var canonicalHeaders = string.Join("\n", headers.OrderBy(h => h.Key).Select(h => $"{h.Key.ToLower()}:{h.Value}")) + "\n";
        var signedHeaders = string.Join(";", headers.Keys.OrderBy(k => k).Select(k => k.ToLower()));
        var payloadHash = ComputeSha256Hash(payload);

        var canonicalRequest = $"{method}\n/{s3Key}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        var credentialScope = $"{dateStamp}/{_region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{credentialScope}\n{ComputeSha256Hash(Encoding.UTF8.GetBytes(canonicalRequest))}";

        var signature = ComputeHmacSha256(
            ComputeHmacSha256(
                ComputeHmacSha256(
                    ComputeHmacSha256(
                        ComputeHmacSha256(Encoding.UTF8.GetBytes($"AWS4{_secretAccessKey}"), dateStamp),
                        _region),
                    "s3"),
                "aws4_request"),
            stringToSign);

        return Convert.ToHexString(signature).ToLower();
    }

    private string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash).ToLower();
    }

    private byte[] ComputeHmacSha256(byte[] key, string data)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }
}