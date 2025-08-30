using FluentValidation;

namespace ByteBook.Application.DTOs.Auth;

public class RegisterUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
}

public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters")
            .Matches("^[a-zA-Z\\s]+$").WithMessage("First name can only contain letters and spaces");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters")
            .Matches("^[a-zA-Z\\s]+$").WithMessage("Last name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.Website)
            .Must(BeValidUrl).WithMessage("Invalid website URL")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.SocialLinks)
            .Must(BeValidSocialLinks).WithMessage("Invalid social media links")
            .When(x => x.SocialLinks != null && x.SocialLinks.Any());
    }

    private bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) 
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    private bool BeValidSocialLinks(Dictionary<string, string>? socialLinks)
    {
        if (socialLinks == null) return true;
        
        var validPlatforms = new[] { "twitter", "linkedin", "facebook", "instagram", "github" };
        
        return socialLinks.All(link => 
            validPlatforms.Contains(link.Key.ToLower()) && 
            BeValidUrl(link.Value));
    }
}