using AutoMapper;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ByteBook.UnitTests.Application.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<AuthenticationService>>();

        _authenticationService = new AuthenticationService(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };

        var userDto = new UserDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Role = "Reader",
            IsEmailVerified = false,
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(registerDto.Email))
            .ReturnsAsync((User?)null);

        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        _mapperMock.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(userDto))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _emailServiceMock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result.Success());

        // Act
        var result = await _authenticationService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        Assert.Equal(userDto, result.Value.User);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(registerDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(registerDto.Email, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnValidationError()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };

        var existingUser = new User("Existing", "User", new Email("john.doe@example.com"), "hashedpassword");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authenticationService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(registerDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "StrongPass123!"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("StrongPass123!");
        var user = new User("John", "Doe", new Email("john.doe@example.com"), hashedPassword);

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

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(userDto);

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(userDto))
            .Returns("access-token");

        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "StrongPass123!"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid email or password", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnValidationError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var user = new User("John", "Doe", new Email("john.doe@example.com"), hashedPassword);

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid email or password", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldReturnValidationError()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "StrongPass123!"
        };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("StrongPass123!");
        var user = new User("John", "Doe", new Email("john.doe@example.com"), hashedPassword);
        user.Deactivate(); // Deactivate the user

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.LoginAsync(loginDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("deactivated", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(loginDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Email = "john.doe@example.com",
            Token = "valid-token"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        // Set the verification token using reflection since it's private
        var tokenProperty = typeof(User).GetProperty("EmailVerificationToken");
        tokenProperty?.SetValue(user, "valid-token");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(verifyDto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(verifyDto);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(verifyDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Email = "nonexistent@example.com",
            Token = "valid-token"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(verifyDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(verifyDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(verifyDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldReturnValidationError()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Email = "john.doe@example.com",
            Token = "invalid-token"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        // Set a different verification token
        var tokenProperty = typeof(User).GetProperty("EmailVerificationToken");
        tokenProperty?.SetValue(user, "different-token");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(verifyDto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(verifyDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(verifyDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "john.doe@example.com"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(forgotPasswordDto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(ByteBook.Application.Common.Result.Success());

        // Act
        var result = await _authenticationService.ForgotPasswordAsync(forgotPasswordDto);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(forgotPasswordDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendPasswordResetAsync(forgotPasswordDto.Email, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldReturnSuccessForSecurity()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "nonexistent@example.com"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(forgotPasswordDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.ForgotPasswordAsync(forgotPasswordDto);

        // Assert - Should return success to not reveal if email exists
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(forgotPasswordDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var userDto = new UserDto 
        { 
            Id = 1, 
            Email = "john.doe@example.com", 
            FirstName = "John", 
            LastName = "Doe",
            Role = "Reader",
            IsEmailVerified = true,
            IsActive = true
        };

        _tokenServiceMock.Setup(x => x.ValidateTokenAsync(refreshTokenDto.RefreshToken, default))
            .ReturnsAsync(ByteBook.Application.Common.Result<UserDto>.Success(userDto));

        _tokenServiceMock.Setup(x => x.GenerateAccessToken(userDto))
            .Returns("new-access-token");
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("new-refresh-token");

        // Act
        var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);

        _tokenServiceMock.Verify(x => x.ValidateTokenAsync(refreshTokenDto.RefreshToken, default), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(userDto), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnFailure()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        _tokenServiceMock.Setup(x => x.ValidateTokenAsync(refreshTokenDto.RefreshToken, default))
            .ReturnsAsync(ByteBook.Application.Common.Result<UserDto>.Failure("Invalid token"));

        // Act
        var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid refresh token", result.ErrorMessage ?? "");

        _tokenServiceMock.Verify(x => x.ValidateTokenAsync(refreshTokenDto.RefreshToken, default), Times.Once);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<UserDto>()), Times.Never);
        _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var resetPasswordDto = new ResetPasswordDto
        {
            Email = "john.doe@example.com",
            Token = "valid-reset-token",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), "hashedpassword");
        user.GeneratePasswordResetToken();
        
        // Set the reset token using reflection
        var tokenProperty = typeof(User).GetProperty("ResetPasswordToken");
        tokenProperty?.SetValue(user, "valid-reset-token");
        
        var expiryProperty = typeof(User).GetProperty("ResetPasswordTokenExpiry");
        expiryProperty?.SetValue(user, DateTime.UtcNow.AddHours(1));

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(resetPasswordDto.Email))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authenticationService.ResetPasswordAsync(resetPasswordDto);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(resetPasswordDto.Email), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_ShouldReturnSuccess()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), BCrypt.Net.BCrypt.HashPassword("CurrentPassword123!"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authenticationService.ChangePasswordAsync(1, changePasswordDto);

        // Assert
        Assert.True(result.IsSuccess);

        _userRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldReturnValidationFailure()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        var user = new User("John", "Doe", new Email("john.doe@example.com"), BCrypt.Net.BCrypt.HashPassword("CurrentPassword123!"));
        _userRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.ChangePasswordAsync(1, changePasswordDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("incorrect", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ShouldReturnValidationFailure()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "CurrentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.ChangePasswordAsync(999, changePasswordDto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage ?? "");

        _userRepositoryMock.Verify(x => x.GetByIdAsync(999), Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnSuccess()
    {
        // Arrange
        var userId = 1;

        // Act
        var result = await _authenticationService.LogoutAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
    }
}