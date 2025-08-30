using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ByteBook.UnitTests.Application.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenBlacklistService> _blacklistServiceMock;
    private readonly Mock<ILogger<TokenService>> _loggerMock;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _blacklistServiceMock = new Mock<ITokenBlacklistService>();
        _loggerMock = new Mock<ILogger<TokenService>>();

        // Setup configuration
        _configurationMock.Setup(x => x["Jwt:Secret"]).Returns("ThisIsAVeryLongSecretKeyForJWTTokenGeneration123456789");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("ByteBook");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("ByteBook-Users");
        _configurationMock.Setup(x => x["Jwt:AccessTokenExpiryMinutes"]).Returns("60");
        _configurationMock.Setup(x => x["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        _tokenService = new TokenService(
            _configurationMock.Object,
            _userRepositoryMock.Object,
            _blacklistServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ShouldReturnValidToken()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        // Act
        var token = _tokenService.GenerateAccessToken(userDto);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        
        Assert.Equal("ByteBook", jsonToken.Issuer);
        Assert.Equal("ByteBook-Users", jsonToken.Audiences.First());
        Assert.Equal("1", jsonToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("john.doe@example.com", jsonToken.Claims.First(x => x.Type == ClaimTypes.Email).Value);
        Assert.Equal("John", jsonToken.Claims.First(x => x.Type == ClaimTypes.GivenName).Value);
        Assert.Equal("Doe", jsonToken.Claims.First(x => x.Type == ClaimTypes.Surname).Value);
        Assert.Equal("Reader", jsonToken.Claims.First(x => x.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        
        // Should be valid base64
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnUserDto()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        var token = _tokenService.GenerateAccessToken(userDto);
        
        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        _blacklistServiceMock.Setup(x => x.IsTokenBlacklistedAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenService.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Id);
        Assert.Equal("john.doe@example.com", result.Value.Email);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithBlacklistedToken_ShouldReturnFailure()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        var token = _tokenService.GenerateAccessToken(userDto);

        _blacklistServiceMock.Setup(x => x.IsTokenBlacklistedAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        // Act
        var result = await _tokenService.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("revoked", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        var token = _tokenService.GenerateAccessToken(userDto);
        
        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        user.Deactivate();
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        _blacklistServiceMock.Setup(x => x.IsTokenBlacklistedAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenService.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("deactivated", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 999,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        var token = _tokenService.GenerateAccessToken(userDto);
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        _blacklistServiceMock.Setup(x => x.IsTokenBlacklistedAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);

        // Act
        var result = await _tokenService.ValidateTokenAsync(token);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _tokenService.ValidateTokenAsync(invalidToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid token", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        var token = _tokenService.GenerateAccessToken(userDto);

        _blacklistServiceMock.Setup(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<DateTime>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _tokenService.RevokeTokenAsync(token);

        // Assert
        Assert.True(result.IsSuccess);
        _blacklistServiceMock.Verify(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Once);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _tokenService.RevokeTokenAsync(invalidToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid token", result.ErrorMessage ?? "");
        _blacklistServiceMock.Verify(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }
}