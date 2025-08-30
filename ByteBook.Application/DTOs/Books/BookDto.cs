namespace ByteBook.Application.DTOs.Books;

public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int TotalPages { get; set; }
    public decimal? PricePerPage { get; set; }
    public decimal? PricePerHour { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public BookAnalyticsDto? Analytics { get; set; }
}

public class BookListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal? PricePerPage { get; set; }
    public decimal? PricePerHour { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public BookRatingDto? Rating { get; set; }
    public int ReadCount { get; set; }
}

public class BookRatingDto
{
    public decimal Average { get; set; }
    public int Count { get; set; }
}

public class BookAnalyticsDto
{
    public int TotalReads { get; set; }
    public int UniqueReaders { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal AverageReadingTime { get; set; }
    public decimal CompletionRate { get; set; }
    public Dictionary<string, int> ReadsByDate { get; set; } = new();
    public Dictionary<string, decimal> EarningsByDate { get; set; } = new();
}