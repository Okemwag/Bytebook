using ByteBook.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ByteBook.UnitTests.Application.Services;

public class TokenBlacklistServiceTests
{
    private readonly Mock<ILogger<TokenBlacklistService>> _loggerMock;
    private readonly TokenBlacklistService _blacklistService;

    public TokenBlacklistServiceTests()
    {
        _loggerMock = new Mock<ILogger<TokenBlacklistService>>();
        _blacklistService = new TokenBlacklistService(_loggerMock.Object);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ShouldAddTokenToBlacklist()
    {
        // Arrange
        var tokenId = "test-token-id";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        await _blacklistService.BlacklistTokenAsync(tokenId, expiry);

        // Assert
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(tokenId);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithNonBlacklistedToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenId = "non-blacklisted-token";

        // Act
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(tokenId);

        // Assert
        Assert.False(isBlacklisted);
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenId = "blacklisted-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        await _blacklistService.BlacklistTokenAsync(tokenId, expiry);

        // Act
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(tokenId);

        // Assert
        Assert.True(isBlacklisted);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var expiredTokenId = "expired-token";
        var validTokenId = "valid-token";
        var expiredTime = DateTime.UtcNow.AddHours(-1); // Expired
        var validTime = DateTime.UtcNow.AddHours(1); // Valid

        await _blacklistService.BlacklistTokenAsync(expiredTokenId, expiredTime);
        await _blacklistService.BlacklistTokenAsync(validTokenId, validTime);

        // Verify both tokens are initially blacklisted
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(expiredTokenId));
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(validTokenId));

        // Act
        await _blacklistService.CleanupExpiredTokensAsync();

        // Assert
        Assert.False(await _blacklistService.IsTokenBlacklistedAsync(expiredTokenId));
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(validTokenId));
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_WithNoExpiredTokens_ShouldNotAffectValidTokens()
    {
        // Arrange
        var validTokenId1 = "valid-token-1";
        var validTokenId2 = "valid-token-2";
        var validTime = DateTime.UtcNow.AddHours(1);

        await _blacklistService.BlacklistTokenAsync(validTokenId1, validTime);
        await _blacklistService.BlacklistTokenAsync(validTokenId2, validTime);

        // Act
        await _blacklistService.CleanupExpiredTokensAsync();

        // Assert
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(validTokenId1));
        Assert.True(await _blacklistService.IsTokenBlacklistedAsync(validTokenId2));
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_WithEmptyBlacklist_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _blacklistService.CleanupExpiredTokensAsync();
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithSameTokenMultipleTimes_ShouldNotDuplicate()
    {
        // Arrange
        var tokenId = "duplicate-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        await _blacklistService.BlacklistTokenAsync(tokenId, expiry);
        await _blacklistService.BlacklistTokenAsync(tokenId, expiry); // Add same token again

        // Assert
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(tokenId);
        Assert.True(isBlacklisted);
    }
}