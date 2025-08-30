using System.Collections.Concurrent;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens;
    private readonly ILogger<TokenBlacklistService> _logger;
    private readonly Timer _cleanupTimer;

    public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
    {
        _blacklistedTokens = new ConcurrentDictionary<string, DateTime>();
        _logger = logger;
        
        // Run cleanup every hour
        _cleanupTimer = new Timer(async _ => await CleanupExpiredTokensAsync(), null, 
            TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public Task BlacklistTokenAsync(string tokenId, DateTime expiry, CancellationToken cancellationToken = default)
    {
        _blacklistedTokens.TryAdd(tokenId, expiry);
        _logger.LogInformation("Token blacklisted: {TokenId}, expires at: {Expiry}", 
            tokenId[..Math.Min(8, tokenId.Length)], expiry);
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        var isBlacklisted = _blacklistedTokens.ContainsKey(tokenId);
        return Task.FromResult(isBlacklisted);
    }

    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredTokens = _blacklistedTokens
            .Where(kvp => kvp.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var tokenId in expiredTokens)
        {
            _blacklistedTokens.TryRemove(tokenId, out _);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

// Redis-based implementation for production use
public class RedisTokenBlacklistService : ITokenBlacklistService
{
    // This would be implemented when Redis is available
    // For now, we'll keep it as a placeholder
    
    public Task BlacklistTokenAsync(string tokenId, DateTime expiry, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Redis implementation not available yet");
    }

    public Task<bool> IsTokenBlacklistedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Redis implementation not available yet");
    }

    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Redis implementation not available yet");
    }
}