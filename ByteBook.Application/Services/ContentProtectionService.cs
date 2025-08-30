using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ByteBook.Application.Services;

public class ContentProtectionService : IContentProtectionService
{
    private readonly IUserRepository _userRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContentProtectionService> _logger;
    private readonly string _watermarkTemplate;
    private readonly TimeSpan _accessTokenExpiry;

    public ContentProtectionService(
        IUserRepository userRepository,
        IBookRepository bookRepository,
        IConfiguration configuration,
        ILogger<ContentProtectionService> logger)
    {
        _userRepository = userRepository;
        _bookRepository = bookRepository;
        _configuration = configuration;
        _logger = logger;
        _watermarkTemplate = _configuration["ContentProtection:WatermarkTemplate"] ?? "© {UserName} - {Email} - {Timestamp}";
        _accessTokenExpiry = TimeSpan.FromMinutes(int.Parse(_configuration["ContentProtection:AccessTokenExpiryMinutes"] ?? "30"));
    }

    public async Task<string> ApplyWatermarkAsync(string content, int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Attempted to apply watermark for non-existent user: {UserId}", userId);
                return content; // Return original content if user not found
            }

            var watermark = _watermarkTemplate
                .Replace("{UserName}", user.GetFullName())
                .Replace("{Email}", user.Email.Value)
                .Replace("{UserId}", userId.ToString())
                .Replace("{Timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));

            // Apply watermark at multiple positions in the content
            var watermarkedContent = ApplyWatermarkToContent(content, watermark);

            _logger.LogDebug("Watermark applied for user {UserId}", userId);

            return watermarkedContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying watermark for user {UserId}", userId);
            return content; // Return original content on error
        }
    }

    public async Task<bool> ValidateContentAccessAsync(int userId, int bookId, int pageNumber)
    {
        try
        {
            // Get user and book
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Content access validation failed: User {UserId} not found", userId);
                return false;
            }

            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("Content access validation failed: Book {BookId} not found", bookId);
                return false;
            }

            // Check if user can access the book
            if (!book.CanBeAccessedBy(userId))
            {
                _logger.LogWarning("Content access denied: User {UserId} cannot access book {BookId}", userId, bookId);
                return false;
            }

            // Validate page number
            if (pageNumber < 1 || pageNumber > book.TotalPages)
            {
                _logger.LogWarning("Content access validation failed: Invalid page number {PageNumber} for book {BookId}", pageNumber, bookId);
                return false;
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Content access denied: User {UserId} account is inactive", userId);
                return false;
            }

            // Additional security checks could be added here:
            // - Check payment status for the specific page/content
            // - Verify reading session is active
            // - Check for suspicious activity patterns
            // - Validate geographic restrictions if any

            _logger.LogDebug("Content access validated for user {UserId}, book {BookId}, page {PageNumber}", userId, bookId, pageNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating content access for user {UserId}, book {BookId}, page {PageNumber}", userId, bookId, pageNumber);
            return false;
        }
    }

    public async Task<string> GenerateSecureContentUrlAsync(int bookId, int pageNumber, int userId)
    {
        try
        {
            // Validate access first
            var hasAccess = await ValidateContentAccessAsync(userId, bookId, pageNumber);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("User does not have access to this content");
            }

            // Generate time-limited access token
            var expiryTime = DateTime.UtcNow.Add(_accessTokenExpiry);
            var tokenData = $"{userId}:{bookId}:{pageNumber}:{expiryTime:yyyy-MM-ddTHH:mm:ssZ}";
            var token = GenerateSecureToken(tokenData);

            // Create secure URL
            var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
            var secureUrl = $"{baseUrl}/api/books/{bookId}/pages/{pageNumber}/content?token={token}&expires={expiryTime:yyyy-MM-ddTHH:mm:ssZ}";

            _logger.LogDebug("Secure content URL generated for user {UserId}, book {BookId}, page {PageNumber}", userId, bookId, pageNumber);

            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating secure content URL for user {UserId}, book {BookId}, page {PageNumber}", userId, bookId, pageNumber);
            throw;
        }
    }

    public async Task LogSuspiciousActivityAsync(int userId, string activity, string details)
    {
        try
        {
            // In a real implementation, this would log to a security monitoring system
            // For now, we'll log to the application logger and could store in database

            var logEntry = new
            {
                UserId = userId,
                Activity = activity,
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = "Unknown", // Would be extracted from HTTP context
                UserAgent = "Unknown"  // Would be extracted from HTTP context
            };

            _logger.LogWarning("Suspicious activity detected: {Activity} by user {UserId}. Details: {Details}", 
                activity, userId, details);

            // TODO: Store in security audit table
            // TODO: Trigger alerts if threshold exceeded
            // TODO: Implement automatic blocking for severe violations

            await Task.CompletedTask; // Satisfy async requirement
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging suspicious activity for user {UserId}", userId);
        }
    }

    private string ApplyWatermarkToContent(string content, string watermark)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var lines = content.Split('\n');
        var watermarkedLines = new List<string>();

        // Add watermark at the beginning
        watermarkedLines.Add($"<!-- {watermark} -->");
        watermarkedLines.Add("");

        // Add content with periodic watermarks
        for (int i = 0; i < lines.Length; i++)
        {
            watermarkedLines.Add(lines[i]);

            // Add invisible watermark every 10 lines
            if ((i + 1) % 10 == 0)
            {
                watermarkedLines.Add($"<!-- {watermark} -->");
            }
        }

        // Add watermark at the end
        watermarkedLines.Add("");
        watermarkedLines.Add($"<!-- {watermark} -->");

        return string.Join('\n', watermarkedLines);
    }

    private string GenerateSecureToken(string data)
    {
        // Generate HMAC-SHA256 token for secure content access
        var secretKey = _configuration["ContentProtection:SecretKey"] ?? "default-content-protection-key";
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        
        // Combine original data with hash for verification
        var tokenBytes = dataBytes.Concat(hashBytes).ToArray();
        return Convert.ToBase64String(tokenBytes);
    }

    public bool ValidateSecureToken(string token, out int userId, out int bookId, out int pageNumber, out DateTime expiryTime)
    {
        userId = 0;
        bookId = 0;
        pageNumber = 0;
        expiryTime = DateTime.MinValue;

        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            
            // Extract hash (last 32 bytes for SHA256)
            if (tokenBytes.Length < 32)
                return false;

            var dataBytes = tokenBytes[..^32];
            var providedHash = tokenBytes[^32..];
            
            // Verify hash
            var secretKey = _configuration["ContentProtection:SecretKey"] ?? "default-content-protection-key";
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var expectedHash = hmac.ComputeHash(dataBytes);
            
            if (!providedHash.SequenceEqual(expectedHash))
                return false;

            // Parse data
            var data = Encoding.UTF8.GetString(dataBytes);
            var parts = data.Split(':');
            
            if (parts.Length != 4)
                return false;

            if (!int.TryParse(parts[0], out userId) ||
                !int.TryParse(parts[1], out bookId) ||
                !int.TryParse(parts[2], out pageNumber) ||
                !DateTime.TryParse(parts[3], out expiryTime))
                return false;

            // Check if token has expired
            if (expiryTime < DateTime.UtcNow)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid secure token provided");
            return false;
        }
    }
}