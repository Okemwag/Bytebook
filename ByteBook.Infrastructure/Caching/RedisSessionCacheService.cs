using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace ByteBook.Infrastructure.Caching;

public class RedisSessionCacheService : ISessionCacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisSessionCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisSessionCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisSessionCacheService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetSessionDataAsync<T>(string sessionId, string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            var value = await _database.HashGetAsync(sessionKey, key);
            
            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session data for session: {SessionId}, key: {Key}", sessionId, key);
            return null;
        }
    }

    public async Task SetSessionDataAsync<T>(string sessionId, string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            await _database.HashSetAsync(sessionKey, key, serializedValue);
            
            if (expiry.HasValue)
            {
                await _database.KeyExpireAsync(sessionKey, expiry.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting session data for session: {SessionId}, key: {Key}", sessionId, key);
        }
    }

    public async Task RemoveSessionDataAsync(string sessionId, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            await _database.HashDeleteAsync(sessionKey, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing session data for session: {SessionId}, key: {Key}", sessionId, key);
        }
    }

    public async Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            await _database.KeyDeleteAsync(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session: {SessionId}", sessionId);
        }
    }

    public async Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            return await _database.KeyExistsAsync(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if session exists: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task ExtendSessionAsync(string sessionId, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        try
        {
            var sessionKey = GetSessionKey(sessionId);
            await _database.KeyExpireAsync(sessionKey, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending session: {SessionId}", sessionId);
        }
    }

    private static string GetSessionKey(string sessionId) => $"session:{sessionId}";
}