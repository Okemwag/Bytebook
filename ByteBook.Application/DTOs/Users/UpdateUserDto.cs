using FluentValidation;

namespace ByteBook.Application.DTOs.Users;

public class UpdateUserProfileDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
    public IFormFile? Avatar { get; set; }
}

public class UserStatsDto
{
    public int TotalBooksAuthored { get; set; }
    public int TotalBooksRead { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalReadingSessions { get; set; }
    public int TotalPagesRead { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public DateTime MemberSince { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters")
            .Matches("^[a-zA-Z\\s]+$").WithMessage("First name can only contain letters and spaces")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters")
            .Matches("^[a-zA-Z\\s]+$").WithMessage("Last name can only contain letters and spaces")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.Website)
            .Must(BeValidUrl).WithMessage("Invalid website URL")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.SocialLinks)
            .Must(BeValidSocialLinks).WithMessage("Invalid social media links")
            .When(x => x.SocialLinks != null && x.SocialLinks.Any());

        RuleFor(x => x.Avatar)
            .Must(BeValidImageFile).WithMessage("Avatar must be a valid image file (JPG, PNG, WebP)")
            .When(x => x.Avatar != null);
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

    private bool BeValidImageFile(IFormFile? file)
    {
        if (file == null) return true;
        
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length > 0;
    }
}

// Temporary interface for file uploads - will be replaced with Microsoft.AspNetCore.Http.IFormFile
public interface IFormFile
{
    string ContentType { get; }
    long Length { get; }
    string FileName { get; }
    Stream OpenReadStream();
}