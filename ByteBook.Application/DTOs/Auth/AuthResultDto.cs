namespace ByteBook.Application.DTOs.Auth;

public class AuthResultDto
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsActive { get; set; }
    public UserProfileDto? Profile { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserProfileDto
{
    public string? Bio { get; set; }
    public string? Avatar { get; set; }
    public string? Website { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
}