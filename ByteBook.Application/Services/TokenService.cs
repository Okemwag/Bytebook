using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Auth;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ByteBook.Application.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<TokenService> _logger;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public TokenService(
        IConfiguration configuration,
        IUserRepository userRepository,
        ITokenBlacklistService blacklistService,
        ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _blacklistService = blacklistService;
        _logger = logger;
        
        _jwtSecret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        _jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        _accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60");
        _refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
    }

    public string GenerateAccessToken(UserDto user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("email_verified", user.IsEmailVerified.ToString()),
            new Claim("is_active", user.IsActive.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<Result<UserDto>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Result<UserDto>.Failure("Invalid token");
            }

            // Check if token is blacklisted
            var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti);
            if (jtiClaim != null)
            {
                var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(jtiClaim.Value, cancellationToken);
                if (isBlacklisted)
                {
                    return Result<UserDto>.Failure("Token has been revoked");
                }
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Result<UserDto>.Failure("Invalid token claims");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("User not found");
            }

            if (!user.IsActive)
            {
                return Result<UserDto>.Failure("User account is deactivated");
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email.Value,
                Role = user.Role.ToString(),
                IsEmailVerified = user.IsEmailVerified,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt == default ? null : user.LastLoginAt,
                Profile = new UserProfileDto
                {
                    Bio = user.Profile.Bio,
                    Avatar = user.Profile.AvatarUrl,
                    Website = user.Profile.Website,
                    SocialLinks = new Dictionary<string, string>
                    {
                        { "twitter", user.Profile.TwitterHandle ?? "" },
                        { "linkedin", user.Profile.LinkedInProfile ?? "" }
                    }.Where(x => !string.IsNullOrEmpty(x.Value)).ToDictionary(x => x.Key, x => x.Value)
                }
            };

            return Result<UserDto>.Success(userDto);
        }
        catch (SecurityTokenExpiredException)
        {
            return Result<UserDto>.Failure("Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Result<UserDto>.Failure("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Result<UserDto>.Failure("Token validation error");
        }
    }

    public async Task<Result> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = false, // Don't validate lifetime for revocation
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return Result.Failure("Invalid token format");
            }

            // Extract JTI (JWT ID) and expiry from token
            var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti);
            if (jtiClaim == null)
            {
                return Result.Failure("Token does not contain JTI claim");
            }

            var expiry = jwtToken.ValidTo;
            
            // Add token to blacklist
            await _blacklistService.BlacklistTokenAsync(jtiClaim.Value, expiry, cancellationToken);
            
            _logger.LogInformation("Token revoked successfully: {TokenId}", 
                jtiClaim.Value[..Math.Min(8, jtiClaim.Value.Length)]);
            
            return Result.Success();
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token provided for revocation");
            return Result.Failure("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Result.Failure("Error revoking token");
        }
    }
}