using ByteBook.Application.Interfaces;
using ByteBook.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteBook.Infrastructure.Caching;

public interface ICachingStrategy
{
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Func<T, bool> shouldCache, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task WarmupAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
}

public class CachingStrategy : ICachingStrategy
{
    private readonly ICacheService _cacheService;
    private readonly IDistributedLockService _lockService;
    private readonly CacheSettings _settings;
    private readonly ILogger<CachingStrategy> _logger;

    public CachingStrategy(
        ICacheService cacheService,
        IDistributedLockService lockService,
        IOptions<CacheSettings> settings,
        ILogger<CachingStrategy> logger)
    {
        _cacheService = cacheService;
        _lockService = lockService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        return await GetOrSetAsync(key, factory, _ => true, expiry, cancellationToken);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, Func<T, bool> shouldCache, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try to get from cache first
            var cached = await _cacheService.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);

            // Use distributed lock to prevent cache stampede
            var lockKey = $"lock:cache:{key}";
            var lockExpiry = _settings.LockExpiry;
            var lockTimeout = _settings.LockTimeout;

            using var distributedLock = await _lockService.AcquireLockAsync(lockKey, lockExpiry, lockTimeout, cancellationToken);
            
            if (distributedLock == null)
            {
                _logger.LogWarning("Failed to acquire lock for cache key: {Key}, returning factory result without caching", key);
                return await factory();
            }

            // Double-check cache after acquiring lock
            cached = await _cacheService.GetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit after lock acquisition for key: {Key}", key);
                return cached;
            }

            // Execute factory and cache result
            var result = await factory();
            
            if (result != null && shouldCache(result))
            {
                var cacheExpiry = expiry ?? _settings.DefaultExpiry;
                await _cacheService.SetAsync(key, result, cacheExpiry, cancellationToken);
                _logger.LogDebug("Cached result for key: {Key} with expiry: {Expiry}", key, cacheExpiry);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            
            // Fallback to factory without caching
            try
            {
                return await factory();
            }
            catch (Exception factoryEx)
            {
                _logger.LogError(factoryEx, "Factory method also failed for key: {Key}", key);
                throw;
            }
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Invalidated cache for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for key: {Key}", key);
        }
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            _logger.LogDebug("Invalidated cache for pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for pattern: {Pattern}", pattern);
        }
    }

    public async Task WarmupAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var exists = await _cacheService.ExistsAsync(key, cancellationToken);
            if (!exists)
            {
                var result = await factory();
                if (result != null)
                {
                    var cacheExpiry = expiry ?? _settings.DefaultExpiry;
                    await _cacheService.SetAsync(key, result, cacheExpiry, cancellationToken);
                    _logger.LogDebug("Warmed up cache for key: {Key}", key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up cache for key: {Key}", key);
        }
    }
}

public static class CacheKeys
{
    // User-related cache keys
    public static string User(int userId) => $"user:{userId}";
    public static string UserByEmail(string email) => $"user:email:{email}";
    public static string UserSessions(int userId) => $"user:{userId}:sessions";
    
    // Book-related cache keys
    public static string Book(int bookId) => $"book:{bookId}";
    public static string BookContent(int bookId, int pageNumber) => $"book:{bookId}:content:{pageNumber}";
    public static string BooksByAuthor(int authorId) => $"books:author:{authorId}";
    public static string BooksByCategory(string category) => $"books:category:{category}";
    public static string BookAnalytics(int bookId) => $"book:{bookId}:analytics";
    
    // Search-related cache keys
    public static string SearchResults(string queryHash) => $"search:results:{queryHash}";
    public static string SearchSuggestions(string query) => $"search:suggestions:{query}";
    public static string PopularBooks() => "books:popular";
    public static string TrendingBooks() => "books:trending";
    
    // Payment-related cache keys
    public static string UserPayments(int userId) => $"payments:user:{userId}";
    public static string AuthorEarnings(int authorId) => $"earnings:author:{authorId}";
    public static string PaymentStats() => "payments:stats";
    
    // Reading session cache keys
    public static string ReadingSession(int userId, int bookId) => $"reading:{userId}:{bookId}";
    public static string ReadingProgress(int userId, int bookId) => $"reading:{userId}:{bookId}:progress";
    
    // System cache keys
    public static string SystemStats() => "system:stats";
    public static string Categories() => "system:categories";
    public static string PopularTags() => "system:tags:popular";
    
    // Rate limiting keys
    public static string RateLimit(string identifier, string action) => $"ratelimit:{identifier}:{action}";
    
    // Token blacklist keys
    public static string BlacklistedToken(string tokenId) => $"blacklist:token:{tokenId}";
}

public static class CachePatterns
{
    public static string UserPattern(int userId) => $"user:{userId}*";
    public static string BookPattern(int bookId) => $"book:{bookId}*";
    public static string AuthorPattern(int authorId) => $"*:author:{authorId}*";
    public static string SearchPattern() => "search:*";
    public static string PaymentPattern(int userId) => $"*payment*:{userId}*";
    public static string ReadingPattern(int userId) => $"reading:{userId}*";
    public static string RateLimitPattern(string identifier) => $"ratelimit:{identifier}*";
}

public class BookCachingService
{
    private readonly ICachingStrategy _cachingStrategy;
    private readonly CacheSettings _settings;

    public BookCachingService(ICachingStrategy cachingStrategy, IOptions<CacheSettings> settings)
    {
        _cachingStrategy = cachingStrategy;
        _settings = settings.Value;
    }

    public async Task<T?> GetBookDataAsync<T>(int bookId, Func<Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        var key = CacheKeys.Book(bookId);
        return await _cachingStrategy.GetOrSetAsync(key, factory, _settings.BookContentExpiry, cancellationToken);
    }

    public async Task<T?> GetBookContentAsync<T>(int bookId, int pageNumber, Func<Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        var key = CacheKeys.BookContent(bookId, pageNumber);
        return await _cachingStrategy.GetOrSetAsync(key, factory, _settings.BookContentExpiry, cancellationToken);
    }

    public async Task InvalidateBookCacheAsync(int bookId, CancellationToken cancellationToken = default)
    {
        var pattern = CachePatterns.BookPattern(bookId);
        await _cachingStrategy.InvalidateByPatternAsync(pattern, cancellationToken);
    }
}

public class UserCachingService
{
    private readonly ICachingStrategy _cachingStrategy;
    private readonly CacheSettings _settings;

    public UserCachingService(ICachingStrategy cachingStrategy, IOptions<CacheSettings> settings)
    {
        _cachingStrategy = cachingStrategy;
        _settings = settings.Value;
    }

    public async Task<T?> GetUserDataAsync<T>(int userId, Func<Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        var key = CacheKeys.User(userId);
        return await _cachingStrategy.GetOrSetAsync(key, factory, _settings.DefaultExpiry, cancellationToken);
    }

    public async Task<T?> GetUserByEmailAsync<T>(string email, Func<Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        var key = CacheKeys.UserByEmail(email);
        return await _cachingStrategy.GetOrSetAsync(key, factory, _settings.DefaultExpiry, cancellationToken);
    }

    public async Task InvalidateUserCacheAsync(int userId, CancellationToken cancellationToken = default)
    {
        var pattern = CachePatterns.UserPattern(userId);
        await _cachingStrategy.InvalidateByPatternAsync(pattern, cancellationToken);
    }
}

public class SearchCachingService
{
    private readonly ICachingStrategy _cachingStrategy;
    private readonly CacheSettings _settings;

    public SearchCachingService(ICachingStrategy cachingStrategy, IOptions<CacheSettings> settings)
    {
        _cachingStrategy = cachingStrategy;
        _settings = settings.Value;
    }

    public async Task<T?> GetSearchResultsAsync<T>(string queryHash, Func<Task<T>> factory, CancellationToken cancellationToken = default) where T : class
    {
        var key = CacheKeys.SearchResults(queryHash);
        return await _cachingStrategy.GetOrSetAsync(key, factory, _settings.SearchResultsExpiry, cancellationToken);
    }

    public async Task InvalidateSearchCacheAsync(CancellationToken cancellationToken = default)
    {
        var pattern = CachePatterns.SearchPattern();
        await _cachingStrategy.InvalidateByPatternAsync(pattern, cancellationToken);
    }
}