using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ByteBook.Infrastructure.Caching;

public class RedisDistributedLockService : IDistributedLockService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisDistributedLockService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<IDistributedLock?> AcquireLockAsync(string key, TimeSpan expiry, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var timeoutTime = timeout.HasValue ? DateTime.UtcNow.Add(timeout.Value) : DateTime.UtcNow.AddSeconds(30);

        try
        {
            while (DateTime.UtcNow < timeoutTime && !cancellationToken.IsCancellationRequested)
            {
                var acquired = await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
                
                if (acquired)
                {
                    _logger.LogDebug("Acquired distributed lock for key: {Key}", key);
                    return new RedisDistributedLock(_database, lockKey, lockValue, expiry, _logger);
                }

                await Task.Delay(100, cancellationToken);
            }

            _logger.LogWarning("Failed to acquire distributed lock for key: {Key} within timeout", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring distributed lock for key: {Key}", key);
            return null;
        }
    }
}

public class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _database;
    private readonly string _lockValue;
    private readonly ILogger _logger;
    private bool _disposed;

    public string Key { get; }
    public DateTime AcquiredAt { get; }
    public TimeSpan Expiry { get; private set; }

    public RedisDistributedLock(IDatabase database, string key, string lockValue, TimeSpan expiry, ILogger logger)
    {
        _database = database;
        Key = key;
        _lockValue = lockValue;
        Expiry = expiry;
        AcquiredAt = DateTime.UtcNow;
        _logger = logger;
    }

    public async Task<bool> ExtendAsync(TimeSpan additionalTime, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RedisDistributedLock));

        try
        {
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('EXPIRE', KEYS[1], ARGV[2])
                else
                    return 0
                end";

            var newExpiry = Expiry.Add(additionalTime);
            var result = await _database.ScriptEvaluateAsync(script, new RedisKey[] { Key }, new RedisValue[] { _lockValue, (int)newExpiry.TotalSeconds });
            
            if (result.ToString() == "1")
            {
                Expiry = newExpiry;
                _logger.LogDebug("Extended distributed lock for key: {Key}", Key);
                return true;
            }

            _logger.LogWarning("Failed to extend distributed lock for key: {Key} - lock may have expired or been released", Key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending distributed lock for key: {Key}", Key);
            return false;
        }
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        try
        {
            const string script = @"
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                else
                    return 0
                end";

            var result = await _database.ScriptEvaluateAsync(script, new RedisKey[] { Key }, new RedisValue[] { _lockValue });
            
            if (result.ToString() == "1")
            {
                _logger.LogDebug("Released distributed lock for key: {Key}", Key);
            }
            else
            {
                _logger.LogWarning("Failed to release distributed lock for key: {Key} - lock may have already expired", Key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing distributed lock for key: {Key}", Key);
        }
        finally
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Task.Run(async () => await ReleaseAsync());
        }
    }
}