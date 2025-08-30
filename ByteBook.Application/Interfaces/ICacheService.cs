namespace ByteBook.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken cancellationToken = default);
    Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<long> DecrementAsync(string key, long value = 1, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<bool> SetIfNotExistsAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;
    Task SetManyAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
}

public interface IDistributedLockService
{
    Task<IDistributedLock?> AcquireLockAsync(string key, TimeSpan expiry, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

public interface IDistributedLock : IDisposable
{
    string Key { get; }
    DateTime AcquiredAt { get; }
    TimeSpan Expiry { get; }
    Task<bool> ExtendAsync(TimeSpan additionalTime, CancellationToken cancellationToken = default);
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}

public interface ISessionCacheService
{
    Task<T?> GetSessionDataAsync<T>(string sessionId, string key, CancellationToken cancellationToken = default) where T : class;
    Task SetSessionDataAsync<T>(string sessionId, string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveSessionDataAsync(string sessionId, string key, CancellationToken cancellationToken = default);
    Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> SessionExistsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task ExtendSessionAsync(string sessionId, TimeSpan expiry, CancellationToken cancellationToken = default);
}