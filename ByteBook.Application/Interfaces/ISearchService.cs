using ByteBook.Application.DTOs.Search;

namespace ByteBook.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResult<BookSearchDto>> SearchBooksAsync(BookSearchRequest request, CancellationToken cancellationToken = default);
    Task<bool> IndexBookAsync(BookIndexDto book, CancellationToken cancellationToken = default);
    Task<bool> UpdateBookIndexAsync(int bookId, BookIndexDto book, CancellationToken cancellationToken = default);
    Task<bool> RemoveBookFromIndexAsync(int bookId, CancellationToken cancellationToken = default);
    Task<bool> BulkIndexBooksAsync(IEnumerable<BookIndexDto> books, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSuggestionsAsync(string query, int maxSuggestions = 10, CancellationToken cancellationToken = default);
    Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteIndexAsync(CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);
}

public interface ISearchIndexService
{
    Task ReindexAllBooksAsync(CancellationToken cancellationToken = default);
    Task ReindexBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task<bool> IsIndexHealthyAsync(CancellationToken cancellationToken = default);
    Task<IndexStatistics> GetIndexStatisticsAsync(CancellationToken cancellationToken = default);
}

public class IndexStatistics
{
    public long TotalDocuments { get; set; }
    public long IndexSizeInBytes { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Status { get; set; } = string.Empty;
}