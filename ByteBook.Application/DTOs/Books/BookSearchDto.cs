using FluentValidation;

namespace ByteBook.Application.DTOs.Books;

public class BookSearchDto
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public string? AuthorName { get; set; }
    public decimal? MinPricePerPage { get; set; }
    public decimal? MaxPricePerPage { get; set; }
    public decimal? MinPricePerHour { get; set; }
    public decimal? MaxPricePerHour { get; set; }
    public bool? IsPublished { get; set; }
    public DateTime? PublishedAfter { get; set; }
    public DateTime? PublishedBefore { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "DESC";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BookSearchResultDto
{
    public List<BookListDto> Books { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class PageContentDto
{
    public int BookId { get; set; }
    public int PageNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string WatermarkedContent { get; set; } = string.Empty;
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public int TotalPages { get; set; }
    public decimal? ChargeAmount { get; set; }
    public string? ChargeType { get; set; }
}

public class BookSearchDtoValidator : AbstractValidator<BookSearchDto>
{
    public BookSearchDtoValidator()
    {
        RuleFor(x => x.Query)
            .MaximumLength(200).WithMessage("Search query cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Query));

        RuleFor(x => x.Category)
            .MaximumLength(50).WithMessage("Category cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.AuthorName)
            .MaximumLength(100).WithMessage("Author name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.AuthorName));

        RuleFor(x => x.MinPricePerPage)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum price per page must be non-negative")
            .When(x => x.MinPricePerPage.HasValue);

        RuleFor(x => x.MaxPricePerPage)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum price per page must be non-negative")
            .GreaterThanOrEqualTo(x => x.MinPricePerPage).WithMessage("Maximum price per page must be greater than or equal to minimum price")
            .When(x => x.MaxPricePerPage.HasValue);

        RuleFor(x => x.MinPricePerHour)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum price per hour must be non-negative")
            .When(x => x.MinPricePerHour.HasValue);

        RuleFor(x => x.MaxPricePerHour)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum price per hour must be non-negative")
            .GreaterThanOrEqualTo(x => x.MinPricePerHour).WithMessage("Maximum price per hour must be greater than or equal to minimum price")
            .When(x => x.MaxPricePerHour.HasValue);

        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("Invalid sort field");

        RuleFor(x => x.SortOrder)
            .Must(BeValidSortOrder).WithMessage("Sort order must be ASC or DESC");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }

    private bool BeValidSortField(string sortBy)
    {
        var validFields = new[] { "Title", "CreatedAt", "PublishedAt", "PricePerPage", "PricePerHour", "ReadCount", "Rating" };
        return validFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase);
    }

    private bool BeValidSortOrder(string sortOrder)
    {
        return sortOrder.Equals("ASC", StringComparison.OrdinalIgnoreCase) || 
               sortOrder.Equals("DESC", StringComparison.OrdinalIgnoreCase);
    }
}