using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;

namespace ByteBook.Application.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthResultDto>> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<Result<AuthResultDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
    Task<Result> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(int userId, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    string GenerateAccessToken(UserDto user);
    string GenerateRefreshToken();
    Task<Result<UserDto>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task<Result> SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken = default);
    Task<Result> SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
    Task<Result> SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default);
}