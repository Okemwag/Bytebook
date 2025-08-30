using ByteBook.Application.Interfaces;
using ByteBook.Infrastructure.Caching;
using ByteBook.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Infrastructure;

public class CachingStrategyTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IDistributedLockService> _mockLockService;
    private readonly Mock<IDistributedLock> _mockLock;
    private readonly Mock<ILogger<CachingStrategy>> _mockLogger;
    private readonly CacheSettings _cacheSettings;
    private readonly CachingStrategy _cachingStrategy;

    public CachingStrategyTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockLockService = new Mock<IDistributedLockService>();
        _mockLock = new Mock<IDistributedLock>();
        _mockLogger = new Mock<ILogger<CachingStrategy>>();
        
        _cacheSettings = new CacheSettings
        {
            DefaultExpiry = TimeSpan.FromMinutes(30),
            LockTimeout = TimeSpan.FromSeconds(30),
            LockExpiry = TimeSpan.FromMinutes(5)
        };

        var options = Options.Create(_cacheSettings);
        _cachingStrategy = new CachingStrategy(_mockCacheService.Object, _mockLockService.Object, options, _mockLogger.Object);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Return_Cached_Value_When_Cache_Hit()
    {
        // Arrange
        var key = "test:key";
        var cachedValue = new TestObject { Id = 1, Name = "Cached" };
        var factoryCalled = false;

        _mockCacheService.Setup(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedValue);

        // Act
        var result = await _cachingStrategy.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestObject { Id = 2, Name = "Factory" });
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedValue.Id, result.Id);
        Assert.Equal(cachedValue.Name, result.Name);
        Assert.False(factoryCalled);
        
        _mockCacheService.Verify(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestObject>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Execute_Factory_And_Cache_Result_When_Cache_Miss()
    {
        // Arrange
        var key = "test:key";
        var factoryValue = new TestObject { Id = 2, Name = "Factory" };
        var factoryCalled = false;

        _mockCacheService.Setup(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestObject?)null);

        _mockLockService.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockLock.Object);

        // Act
        var result = await _cachingStrategy.GetOrSetAsync(key, () =>
        {
            factoryCalled = true;
            return Task.FromResult(factoryValue);
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(factoryValue.Id, result.Id);
        Assert.Equal(factoryValue.Name, result.Name);
        Assert.True(factoryCalled);
        
        _mockCacheService.Verify(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockCacheService.Verify(x => x.SetAsync(key, factoryValue, _cacheSettings.DefaultExpiry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Not_Cache_When_ShouldCache_Returns_False()
    {
        // Arrange
        var key = "test:key";
        var factoryValue = new TestObject { Id = 2, Name = "Factory" };

        _mockCacheService.Setup(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestObject?)null);

        _mockLockService.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockLock.Object);

        // Act
        var result = await _cachingStrategy.GetOrSetAsync(key, 
            () => Task.FromResult(factoryValue),
            _ => false); // Don't cache

        // Assert
        Assert.NotNull(result);
        Assert.Equal(factoryValue.Id, result.Id);
        
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestObject>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrSetAsync_Should_Return_Factory_Result_When_Lock_Acquisition_Fails()
    {
        // Arrange
        var key = "test:key";
        var factoryValue = new TestObject { Id = 2, Name = "Factory" };

        _mockCacheService.Setup(x => x.GetAsync<TestObject>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestObject?)null);

        _mockLockService.Setup(x => x.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedLock?)null);

        // Act
        var result = await _cachingStrategy.GetOrSetAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(factoryValue.Id, result.Id);
        
        // Should not attempt to cache when lock acquisition fails
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestObject>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InvalidateAsync_Should_Remove_Cache_Entry()
    {
        // Arrange
        var key = "test:key";

        // Act
        await _cachingStrategy.InvalidateAsync(key);

        // Assert
        _mockCacheService.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateByPatternAsync_Should_Remove_Cache_Entries_By_Pattern()
    {
        // Arrange
        var pattern = "test:*";

        // Act
        await _cachingStrategy.InvalidateByPatternAsync(pattern);

        // Assert
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(pattern, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmupAsync_Should_Cache_Factory_Result_When_Key_Does_Not_Exist()
    {
        // Arrange
        var key = "test:key";
        var factoryValue = new TestObject { Id = 1, Name = "Warmup" };

        _mockCacheService.Setup(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _cachingStrategy.WarmupAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(key, factoryValue, _cacheSettings.DefaultExpiry, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WarmupAsync_Should_Not_Cache_When_Key_Already_Exists()
    {
        // Arrange
        var key = "test:key";
        var factoryValue = new TestObject { Id = 1, Name = "Warmup" };

        _mockCacheService.Setup(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _cachingStrategy.WarmupAsync(key, () => Task.FromResult(factoryValue));

        // Assert
        _mockCacheService.Verify(x => x.ExistsAsync(key, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<TestObject>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CacheKeys_Should_Generate_Correct_Keys()
    {
        // Test various cache key generation methods
        Assert.Equal("user:123", CacheKeys.User(123));
        Assert.Equal("user:email:test@example.com", CacheKeys.UserByEmail("test@example.com"));
        Assert.Equal("book:456", CacheKeys.Book(456));
        Assert.Equal("book:456:content:10", CacheKeys.BookContent(456, 10));
        Assert.Equal("books:author:789", CacheKeys.BooksByAuthor(789));
        Assert.Equal("books:category:Technology", CacheKeys.BooksByCategory("Technology"));
        Assert.Equal("search:results:abc123", CacheKeys.SearchResults("abc123"));
        Assert.Equal("reading:123:456", CacheKeys.ReadingSession(123, 456));
        Assert.Equal("ratelimit:user123:login", CacheKeys.RateLimit("user123", "login"));
    }

    [Fact]
    public void CachePatterns_Should_Generate_Correct_Patterns()
    {
        // Test various cache pattern generation methods
        Assert.Equal("user:123*", CachePatterns.UserPattern(123));
        Assert.Equal("book:456*", CachePatterns.BookPattern(456));
        Assert.Equal("*:author:789*", CachePatterns.AuthorPattern(789));
        Assert.Equal("search:*", CachePatterns.SearchPattern());
        Assert.Equal("*payment*:123*", CachePatterns.PaymentPattern(123));
        Assert.Equal("reading:123*", CachePatterns.ReadingPattern(123));
        Assert.Equal("ratelimit:user123*", CachePatterns.RateLimitPattern("user123"));
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}