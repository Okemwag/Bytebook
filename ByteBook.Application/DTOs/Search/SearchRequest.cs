namespace ByteBook.Application.DTOs.Search;

public class BookSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int? AuthorId { get; set; }
    public decimal? MinPricePerPage { get; set; }
    public decimal? MaxPricePerPage { get; set; }
    public decimal? MinPricePerHour { get; set; }
    public decimal? MaxPricePerHour { get; set; }
    public float? MinRating { get; set; }
    public DateTime? PublishedAfter { get; set; }
    public DateTime? PublishedBefore { get; set; }
    public SearchSortBy SortBy { get; set; } = SearchSortBy.Relevance;
    public SortOrder SortOrder { get; set; } = SortOrder.Descending;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeContent { get; set; } = false; // Whether to search in full content
}

public enum SearchSortBy
{
    Relevance,
    PublishedDate,
    Rating,
    Popularity,
    PricePerPage,
    PricePerHour,
    Title
}

public enum SortOrder
{
    Ascending,
    Descending
}

public class SearchResult<T>
{
    public List<T> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
    public TimeSpan SearchTime { get; set; }
    public List<SearchFacet> Facets { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class SearchFacet
{
    public string Name { get; set; } = string.Empty;
    public List<FacetValue> Values { get; set; } = new();
}

public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
}

public class SearchAnalyticsDto
{
    public long TotalSearches { get; set; }
    public List<PopularQuery> PopularQueries { get; set; } = new();
    public List<CategoryStats> CategoryStats { get; set; } = new();
    public double AverageResultsPerSearch { get; set; }
    public TimeSpan AverageSearchTime { get; set; }
}

public class PopularQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public double ClickThroughRate { get; set; }
}

public class CategoryStats
{
    public string Category { get; set; } = string.Empty;
    public long SearchCount { get; set; }
    public long ResultCount { get; set; }
}