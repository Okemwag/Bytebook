namespace ByteBook.Application.DTOs.Search;

public class BookSearchDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AuthorDto Author { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public PricingDto Pricing { get; set; } = new();
    public RatingsDto Ratings { get; set; } = new();
    public DateTime PublishedAt { get; set; }
    public float Popularity { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public float Score { get; set; } // Search relevance score
}

public class AuthorDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}

public class PricingDto
{
    public decimal? PerPage { get; set; }
    public decimal? PerHour { get; set; }
}

public class RatingsDto
{
    public float Average { get; set; }
    public int Count { get; set; }
}

public class BookIndexDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // Full text content for search
    public AuthorDto Author { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public PricingDto Pricing { get; set; } = new();
    public RatingsDto Ratings { get; set; } = new();
    public DateTime PublishedAt { get; set; }
    public float Popularity { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public int TotalPages { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
}