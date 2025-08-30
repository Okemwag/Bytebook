using ByteBook.Api.Controllers;
using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ByteBook.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Register_With_Valid_Data_Should_Return_Created()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var authResult = new AuthResultDto
        {
            IsSuccess = true,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = new UserDto
            {
                Id = 1,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            }
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Success(authResult));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var returnedAuthResult = Assert.IsType<AuthResultDto>(createdResult.Value);
        Assert.Equal(authResult.AccessToken, returnedAuthResult.AccessToken);
        Assert.Equal(authResult.User.Email, returnedAuthResult.User.Email);
    }

    [Fact]
    public async Task Register_With_Invalid_Data_Should_Return_BadRequest()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email",
            Password = "weak",
            ConfirmPassword = "different"
        };

        var validationErrors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Invalid email format" },
            ["Password"] = new[] { "Password is too weak" }
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Failure("Validation failed", validationErrors));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Should_Return_Ok()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var authResult = new AuthResultDto
        {
            IsSuccess = true,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = new UserDto
            {
                Id = 1,
                Email = loginDto.Email,
                FirstName = "John",
                LastName = "Doe"
            }
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Success(authResult));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAuthResult = Assert.IsType<AuthResultDto>(okResult.Value);
        Assert.Equal(authResult.AccessToken, returnedAuthResult.AccessToken);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Should_Return_Unauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword"
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Failure("Invalid credentials"));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_With_Valid_Token_Should_Return_Ok()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Token = "valid-token"
        };

        _mockAuthService.Setup(x => x.VerifyEmailAsync(It.IsAny<VerifyEmailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.VerifyEmail(verifyDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task VerifyEmail_With_Invalid_Token_Should_Return_BadRequest()
    {
        // Arrange
        var verifyDto = new VerifyEmailDto
        {
            Token = "invalid-token"
        };

        _mockAuthService.Setup(x => x.VerifyEmailAsync(It.IsAny<VerifyEmailDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Invalid token"));

        // Act
        var result = await _controller.VerifyEmail(verifyDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_With_Valid_Email_Should_Return_Ok()
    {
        // Arrange
        var forgotPasswordDto = new ForgotPasswordDto
        {
            Email = "john.doe@example.com"
        };

        _mockAuthService.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.ForgotPassword(forgotPasswordDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetProfile_With_Valid_User_Should_Return_Ok()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "john.doe@example.com"),
            new Claim("given_name", "John"),
            new Claim("family_name", "Doe"),
            new Claim(ClaimTypes.Role, "Reader"),
            new Claim("email_verified", "true")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userDto = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(1, userDto.Id);
        Assert.Equal("john.doe@example.com", userDto.Email);
        Assert.Equal("John", userDto.FirstName);
        Assert.Equal("Doe", userDto.LastName);
    }

    [Fact]
    public void Health_Should_Return_Ok()
    {
        // Act
        var result = _controller.Health();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task RefreshToken_With_Valid_Token_Should_Return_Ok()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var authResult = new AuthResultDto
        {
            IsSuccess = true,
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token"
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Success(authResult));

        // Act
        var result = await _controller.RefreshToken(refreshTokenDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedAuthResult = Assert.IsType<AuthResultDto>(okResult.Value);
        Assert.Equal(authResult.AccessToken, returnedAuthResult.AccessToken);
    }

    [Fact]
    public async Task RefreshToken_With_Invalid_Token_Should_Return_Unauthorized()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResultDto>.Failure("Invalid refresh token"));

        // Act
        var result = await _controller.RefreshToken(refreshTokenDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }
}