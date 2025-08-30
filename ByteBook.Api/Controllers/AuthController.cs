using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ByteBook.Application.Interfaces;
using ByteBook.Application.DTOs.Auth;
using System.Security.Claims;

namespace ByteBook.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="dto">User registration information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", dto.Email);

        var result = await _authenticationService.RegisterAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User registered successfully: {Email}", dto.Email);
            return CreatedAtAction(nameof(GetProfile), new { }, result.Value);
        }

        _logger.LogWarning("User registration failed for email: {Email}. Error: {Error}", dto.Email, result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Registration failed",
            Errors = result.ValidationErrors
        });
    }

    /// <summary>
    /// Authenticate user and get access tokens
    /// </summary>
    /// <param name="dto">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

        var result = await _authenticationService.LoginAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);
            return Ok(result.Value);
        }

        _logger.LogWarning("Login failed for email: {Email}. Error: {Error}", dto.Email, result.Error);
        return Unauthorized(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 401,
            Message = result.Error ?? "Invalid credentials"
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="dto">Refresh token information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Token refresh attempt");

        var result = await _authenticationService.RefreshTokenAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Token refreshed successfully");
            return Ok(result.Value);
        }

        _logger.LogWarning("Token refresh failed. Error: {Error}", result.Error);
        return Unauthorized(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 401,
            Message = result.Error ?? "Invalid refresh token"
        });
    }

    /// <summary>
    /// Verify user email address
    /// </summary>
    /// <param name="dto">Email verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email verification attempt");

        var result = await _authenticationService.VerifyEmailAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Email verified successfully");
            return Ok(new { message = "Email verified successfully" });
        }

        _logger.LogWarning("Email verification failed. Error: {Error}", result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Email verification failed"
        });
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    /// <param name="dto">Email address for password reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset request result</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset request for email: {Email}", dto.Email);

        var result = await _authenticationService.ForgotPasswordAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset email sent to: {Email}", dto.Email);
            return Ok(new { message = "Password reset email sent successfully" });
        }

        _logger.LogWarning("Password reset request failed for email: {Email}. Error: {Error}", dto.Email, result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Password reset request failed"
        });
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="dto">Password reset information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset result</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset attempt");

        var result = await _authenticationService.ResetPasswordAsync(dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password reset successfully");
            return Ok(new { message = "Password reset successfully" });
        }

        _logger.LogWarning("Password reset failed. Error: {Error}", result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Password reset failed"
        });
    }

    /// <summary>
    /// Change user password (requires authentication)
    /// </summary>
    /// <param name="dto">Password change information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password change result</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Password change attempt for user: {UserId}", userId);

        var result = await _authenticationService.ChangePasswordAsync(userId, dto, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }

        _logger.LogWarning("Password change failed for user: {UserId}. Error: {Error}", userId, result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Password change failed"
        });
    }

    /// <summary>
    /// Logout user and invalidate tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Logout attempt for user: {UserId}", userId);

        var result = await _authenticationService.LogoutAsync(userId, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }

        _logger.LogWarning("Logout failed for user: {UserId}. Error: {Error}", userId, result.Error);
        return BadRequest(new ByteBook.Api.Middlewares.ErrorResponse
        {
            StatusCode = 400,
            Message = result.Error ?? "Logout failed"
        });
    }

    /// <summary>
    /// Get current user profile (requires authentication)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        
        _logger.LogInformation("Profile request for user: {UserId}", userId);

        // For now, return basic user info from claims
        // In a full implementation, you'd fetch from the user service
        var userDto = new UserDto
        {
            Id = userId,
            Email = userEmail,
            FirstName = User.FindFirst("given_name")?.Value ?? "",
            LastName = User.FindFirst("family_name")?.Value ?? "",
            Role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Reader",
            IsEmailVerified = bool.Parse(User.FindFirst("email_verified")?.Value ?? "false"),
            IsActive = true
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Check if the API is healthy
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    private string GetCurrentUserEmail()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("Invalid email in token");
        }
        return email;
    }
}