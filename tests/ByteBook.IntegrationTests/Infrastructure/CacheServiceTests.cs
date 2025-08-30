using ByteBook.Application.Interfaces;
using ByteBook.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Xunit;

namespace ByteBook.IntegrationTests.Infrastructure;

public class CacheServiceTests : IClassFixture<RedisTestFixture>
{
    private readonly ICacheService _cacheService;
    private readonly IDistributedLockService _lockService;
    private readonly ISessionCacheService _sessionService;

    public CacheServiceTests(RedisTestFixture fixture)
    {
        _cacheService = fixture.ServiceProvider.GetRequiredService<ICacheService>();
        _lockService = fixture.ServiceProvider.GetRequiredService<IDistributedLockService>();
        _sessionService = fixture.ServiceProvider.GetRequiredService<ISessionCacheService>();
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Work_Correctly()
    {
        // Arrange
        var key = $"test:cache:{Guid.NewGuid()}";
        var testObject = new TestCacheObject { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow };

        // Act
        await _cacheService.SetAsync(key, testObject, TimeSpan.FromMinutes(5));
        var retrieved = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(testObject.Id, retrieved.Id);
        Assert.Equal(testObject.Name, retrieved.Name);
        Assert.Equal(testObject.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), retrieved.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

        // Cleanup
        await _cacheService.RemoveAsync(key);
    }

    [Fact]
    public async Task SetStringAsync_And_GetStringAsync_Should_Work_Correctly()
    {
        // Arrange
        var key = $"test:string:{Guid.NewGuid()}";
        var value = "Test string value";

        // Act
        await _cacheService.SetStringAsync(key, value, TimeSpan.FromMinutes(5));
        var retrieved = await _cacheService.GetStringAsync(key);

        // Assert
        Assert.Equal(value, retrieved);

        // Cleanup
        await _cacheService.RemoveAsync(key);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_Correct_Values()
    {
        // Arrange
        var key = $"test:exists:{Guid.NewGuid()}";
        var value = "Test value";

        // Act & Assert - Key should not exist initially
        var existsBeforeSet = await _cacheService.ExistsAsync(key);
        Assert.False(existsBeforeSet);

        // Set the value
        await _cacheService.SetStringAsync(key, value, TimeSpan.FromMinutes(5));

        // Key should exist now
        var existsAfterSet = await _cacheService.ExistsAsync(key);
        Assert.True(existsAfterSet);

        // Remove the key
        await _cacheService.RemoveAsync(key);

        // Key should not exist after removal
        var existsAfterRemove = await _cacheService.ExistsAsync(key);
        Assert.False(existsAfterRemove);
    }

    [Fact]
    public async Task IncrementAsync_Should_Work_Correctly()
    {
        // Arrange
        var key = $"test:increment:{Guid.NewGuid()}";

        // Act
        var result1 = await _cacheService.IncrementAsync(key, 1, TimeSpan.FromMinutes(5));
        var result2 = await _cacheService.IncrementAsync(key, 5);
        var result3 = await _cacheService.IncrementAsync(key);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(6, result2);
        Assert.Equal(7, result3);

        // Cleanup
        await _cacheService.RemoveAsync(key);
    }

    [Fact]
    public async Task SetIfNotExistsAsync_Should_Work_Correctly()
    {
        // Arrange
        var key = $"test:setnx:{Guid.NewGuid()}";
        var value1 = new TestCacheObject { Id = 1, Name = "First" };
        var value2 = new TestCacheObject { Id = 2, Name = "Second" };

        // Act
        var setResult1 = await _cacheService.SetIfNotExistsAsync(key, value1, TimeSpan.FromMinutes(5));
        var setResult2 = await _cacheService.SetIfNotExistsAsync(key, value2, TimeSpan.FromMinutes(5));
        var retrieved = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.True(setResult1);
        Assert.False(setResult2);
        Assert.NotNull(retrieved);
        Assert.Equal(value1.Id, retrieved.Id);
        Assert.Equal(value1.Name, retrieved.Name);

        // Cleanup
        await _cacheService.RemoveAsync(key);
    }

    [Fact]
    public async Task GetManyAsync_And_SetManyAsync_Should_Work_Correctly()
    {
        // Arrange
        var keyPrefix = $"test:many:{Guid.NewGuid()}";
        var data = new Dictionary<string, TestCacheObject>
        {
            [$"{keyPrefix}:1"] = new TestCacheObject { Id = 1, Name = "First" },
            [$"{keyPrefix}:2"] = new TestCacheObject { Id = 2, Name = "Second" },
            [$"{keyPrefix}:3"] = new TestCacheObject { Id = 3, Name = "Third" }
        };

        // Act
        await _cacheService.SetManyAsync(data, TimeSpan.FromMinutes(5));
        var retrieved = await _cacheService.GetManyAsync<TestCacheObject>(data.Keys);

        // Assert
        Assert.Equal(data.Count, retrieved.Count);
        foreach (var kvp in data)
        {
            Assert.True(retrieved.ContainsKey(kvp.Key));
            Assert.NotNull(retrieved[kvp.Key]);
            Assert.Equal(kvp.Value.Id, retrieved[kvp.Key]!.Id);
            Assert.Equal(kvp.Value.Name, retrieved[kvp.Key]!.Name);
        }

        // Cleanup
        foreach (var key in data.Keys)
        {
            await _cacheService.RemoveAsync(key);
        }
    }

    [Fact]
    public async Task DistributedLock_Should_Work_Correctly()
    {
        // Arrange
        var lockKey = $"test:lock:{Guid.NewGuid()}";
        var expiry = TimeSpan.FromSeconds(30);

        // Act
        using var lock1 = await _lockService.AcquireLockAsync(lockKey, expiry, TimeSpan.FromSeconds(5));
        var lock2 = await _lockService.AcquireLockAsync(lockKey, expiry, TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.NotNull(lock1);
        Assert.Null(lock2); // Should not be able to acquire the same lock

        // Test lock extension
        var extended = await lock1.ExtendAsync(TimeSpan.FromSeconds(30));
        Assert.True(extended);

        // Lock should be released when disposed
    }

    [Fact]
    public async Task SessionCache_Should_Work_Correctly()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var key = "user_data";
        var userData = new TestCacheObject { Id = 123, Name = "Test User" };

        // Act
        await _sessionService.SetSessionDataAsync(sessionId, key, userData, TimeSpan.FromHours(1));
        var retrieved = await _sessionService.GetSessionDataAsync<TestCacheObject>(sessionId, key);
        var sessionExists = await _sessionService.SessionExistsAsync(sessionId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(userData.Id, retrieved.Id);
        Assert.Equal(userData.Name, retrieved.Name);
        Assert.True(sessionExists);

        // Test session extension
        await _sessionService.ExtendSessionAsync(sessionId, TimeSpan.FromHours(2));

        // Test session cleanup
        await _sessionService.ClearSessionAsync(sessionId);
        var existsAfterClear = await _sessionService.SessionExistsAsync(sessionId);
        Assert.False(existsAfterClear);
    }

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

public class RedisTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisTestFixture()
    {
        var services = new ServiceCollection();
        
        // Configure Redis for testing
        var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
        
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "ByteBookTest";
        });

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<ISessionCacheService, RedisSessionCacheService>();

        ServiceProvider = services.BuildServiceProvider();
        _connectionMultiplexer = ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
    }

    public void Dispose()
    {
        _connectionMultiplexer?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }
}