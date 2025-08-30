using AutoMapper;
using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResultDto>> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return Result<AuthResultDto>.ValidationFailure("Email", "A user with this email already exists");
            }

            // Hash password
            var passwordHash = HashPassword(dto.Password);

            // Create email value object
            var email = new Email(dto.Email);

            // Create user
            var user = new User(dto.FirstName, dto.LastName, email, passwordHash);

            // Save user
            var savedUser = await _userRepository.AddAsync(user);

            // Send verification email
            await _emailService.SendEmailVerificationAsync(dto.Email, user.EmailVerificationToken!);

            // Map to DTO
            var userDto = _mapper.Map<UserDto>(savedUser);

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userDto);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("User registered successfully: {Email}", dto.Email);

            return Result<AuthResultDto>.Success(new AuthResultDto
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for email: {Email}", dto.Email);
            return Result<AuthResultDto>.Failure("An error occurred during registration");
        }
    }

    public async Task<Result<AuthResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user by email
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return Result<AuthResultDto>.ValidationFailure("Email", "Invalid email or password");
            }

            // Verify password
            if (!VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Result<AuthResultDto>.ValidationFailure("Password", "Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<AuthResultDto>.ValidationFailure("Account", "Account is deactivated");
            }

            // Update last login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user);

            // Map to DTO
            var userDto = _mapper.Map<UserDto>(user);

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(userDto);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

            return Result<AuthResultDto>.Success(new AuthResultDto
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for email: {Email}", dto.Email);
            return Result<AuthResultDto>.Failure("An error occurred during login");
        }
    }

    public async Task<Result<AuthResultDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate refresh token and get user
            var userResult = await _tokenService.ValidateTokenAsync(dto.RefreshToken, cancellationToken);
            if (userResult.IsFailure)
            {
                return Result<AuthResultDto>.Failure("Invalid refresh token");
            }

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(userResult.Value!);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return Result<AuthResultDto>.Success(new AuthResultDto
            {
                IsSuccess = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userResult.Value
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token refresh");
            return Result<AuthResultDto>.Failure("An error occurred during token refresh");
        }
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return Result.ValidationFailure("Email", "User not found");
            }

            user.VerifyEmail(dto.Token);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Email verified successfully for user: {Email}", dto.Email);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.ValidationFailure("Token", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during email verification for: {Email}", dto.Email);
            return Result.Failure("An error occurred during email verification");
        }
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal if email exists or not for security
                return Result.Success();
            }

            user.GeneratePasswordResetToken();
            await _userRepository.UpdateAsync(user);

            await _emailService.SendPasswordResetAsync(dto.Email, user.ResetPasswordToken!);

            _logger.LogInformation("Password reset requested for user: {Email}", dto.Email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset request for: {Email}", dto.Email);
            return Result.Failure("An error occurred while processing password reset request");
        }
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                return Result.ValidationFailure("Email", "User not found");
            }

            var passwordHash = HashPassword(dto.NewPassword);
            user.ResetPassword(dto.Token, passwordHash);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password reset successfully for user: {Email}", dto.Email);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.ValidationFailure("Token", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password reset for: {Email}", dto.Email);
            return Result.Failure("An error occurred during password reset");
        }
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result.ValidationFailure("User", "User not found");
            }

            // Verify current password
            if (!VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return Result.ValidationFailure("CurrentPassword", "Current password is incorrect");
            }

            // Update password
            var newPasswordHash = HashPassword(dto.NewPassword);
            user.ChangePassword(newPasswordHash);

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during password change for user: {UserId}", userId);
            return Result.Failure("An error occurred during password change");
        }
    }

    public async Task<Result> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, you might want to:
            // 1. Invalidate refresh tokens
            // 2. Add token to blacklist
            // 3. Log the logout event

            _logger.LogInformation("User logged out: {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during logout for user: {UserId}", userId);
            return Result.Failure("An error occurred during logout");
        }
    }

    private string HashPassword(string password)
    {
        // In a real implementation, use BCrypt or similar
        // For now, using a simple hash (NOT for production)
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool VerifyPassword(string password, string hash)
    {
        // In a real implementation, use BCrypt or similar
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}