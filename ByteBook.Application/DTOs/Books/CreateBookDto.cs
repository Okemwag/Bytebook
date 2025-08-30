using FluentValidation;

namespace ByteBook.Application.DTOs.Books;

// Temporary interface for file uploads - will be replaced with Microsoft.AspNetCore.Http.IFormFile
public interface IFormFile
{
    string ContentType { get; }
    long Length { get; }
    string FileName { get; }
    Stream OpenReadStream();
}

public class CreateBookDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal? PricePerPage { get; set; }
    public decimal? PricePerHour { get; set; }
    public string? Content { get; set; }
    public IFormFile? ContentFile { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public class UpdateBookDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal? PricePerPage { get; set; }
    public decimal? PricePerHour { get; set; }
    public string? Content { get; set; }
    public IFormFile? ContentFile { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public class PublishBookDto
{
    public int BookId { get; set; }
    public decimal? PricePerPage { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class CreateBookDtoValidator : AbstractValidator<CreateBookDto>
{
    public CreateBookDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(3, 200).WithMessage("Title must be between 3 and 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .Length(10, 2000).WithMessage("Description must be between 10 and 2000 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .Length(2, 50).WithMessage("Category must be between 2 and 50 characters");

        RuleFor(x => x.PricePerPage)
            .GreaterThan(0).WithMessage("Price per page must be greater than 0")
            .LessThanOrEqualTo(1.00m).WithMessage("Price per page cannot exceed $1.00")
            .When(x => x.PricePerPage.HasValue);

        RuleFor(x => x.PricePerHour)
            .GreaterThan(0).WithMessage("Price per hour must be greater than 0")
            .LessThanOrEqualTo(50.00m).WithMessage("Price per hour cannot exceed $50.00")
            .When(x => x.PricePerHour.HasValue);

        RuleFor(x => x)
            .Must(HaveAtLeastOnePricingModel).WithMessage("At least one pricing model (per page or per hour) must be specified");

        RuleFor(x => x)
            .Must(HaveContentOrFile).WithMessage("Either content text or content file must be provided");

        RuleFor(x => x.ContentFile)
            .Must(BeValidContentFile).WithMessage("Content file must be a PDF")
            .When(x => x.ContentFile != null);

        RuleFor(x => x.CoverImage)
            .Must(BeValidImageFile).WithMessage("Cover image must be a valid image file (JPG, PNG, WebP)")
            .When(x => x.CoverImage != null);
    }

    private bool HaveAtLeastOnePricingModel(CreateBookDto dto)
    {
        return dto.PricePerPage.HasValue || dto.PricePerHour.HasValue;
    }

    private bool HaveContentOrFile(CreateBookDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Content) || dto.ContentFile != null;
    }

    private bool BeValidContentFile(IFormFile? file)
    {
        if (file == null) return true;
        
        var allowedTypes = new[] { "application/pdf" };
        return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length > 0;
    }

    private bool BeValidImageFile(IFormFile? file)
    {
        if (file == null) return true;
        
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length > 0;
    }
}

public class UpdateBookDtoValidator : AbstractValidator<UpdateBookDto>
{
    public UpdateBookDtoValidator()
    {
        RuleFor(x => x.Title)
            .Length(3, 200).WithMessage("Title must be between 3 and 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .Length(10, 2000).WithMessage("Description must be between 10 and 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Category)
            .Length(2, 50).WithMessage("Category must be between 2 and 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.PricePerPage)
            .GreaterThan(0).WithMessage("Price per page must be greater than 0")
            .LessThanOrEqualTo(1.00m).WithMessage("Price per page cannot exceed $1.00")
            .When(x => x.PricePerPage.HasValue);

        RuleFor(x => x.PricePerHour)
            .GreaterThan(0).WithMessage("Price per hour must be greater than 0")
            .LessThanOrEqualTo(50.00m).WithMessage("Price per hour cannot exceed $50.00")
            .When(x => x.PricePerHour.HasValue);

        RuleFor(x => x.ContentFile)
            .Must(BeValidContentFile).WithMessage("Content file must be a PDF")
            .When(x => x.ContentFile != null);

        RuleFor(x => x.CoverImage)
            .Must(BeValidImageFile).WithMessage("Cover image must be a valid image file (JPG, PNG, WebP)")
            .When(x => x.CoverImage != null);
    }

    private bool BeValidContentFile(IFormFile? file)
    {
        if (file == null) return true;
        
        var allowedTypes = new[] { "application/pdf" };
        return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length > 0;
    }

    private bool BeValidImageFile(IFormFile? file)
    {
        if (file == null) return true;
        
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        return allowedTypes.Contains(file.ContentType.ToLower()) && file.Length > 0;
    }
}

public class PublishBookDtoValidator : AbstractValidator<PublishBookDto>
{
    public PublishBookDtoValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Valid book ID is required");

        RuleFor(x => x.PricePerPage)
            .GreaterThan(0).WithMessage("Price per page must be greater than 0")
            .LessThanOrEqualTo(1.00m).WithMessage("Price per page cannot exceed $1.00")
            .When(x => x.PricePerPage.HasValue);

        RuleFor(x => x.PricePerHour)
            .GreaterThan(0).WithMessage("Price per hour must be greater than 0")
            .LessThanOrEqualTo(50.00m).WithMessage("Price per hour cannot exceed $50.00")
            .When(x => x.PricePerHour.HasValue);

        RuleFor(x => x)
            .Must(HaveAtLeastOnePricingModel).WithMessage("At least one pricing model (per page or per hour) must be specified");
    }

    private bool HaveAtLeastOnePricingModel(PublishBookDto dto)
    {
        return dto.PricePerPage.HasValue || dto.PricePerHour.HasValue;
    }
}