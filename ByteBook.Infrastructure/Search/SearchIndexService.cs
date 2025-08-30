using ByteBook.Application.DTOs.Search;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace ByteBook.Infrastructure.Search;

public class SearchIndexService : ISearchIndexService
{
    private readonly ISearchService _searchService;
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly ElasticsearchClient _client;
    private readonly ILogger<SearchIndexService> _logger;
    private readonly string _indexName = "bytebook-books";

    public SearchIndexService(
        ISearchService searchService,
        IBookRepository bookRepository,
        IUserRepository userRepository,
        ElasticsearchClient client,
        ILogger<SearchIndexService> logger)
    {
        _searchService = searchService;
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _client = client;
        _logger = logger;
    }

    public async Task ReindexAllBooksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting full reindex of all books");

            // Get all published books
            var books = await _bookRepository.GetAllPublishedBooksAsync();
            var bookIndexDtos = new List<BookIndexDto>();

            foreach (var book in books)
            {
                var author = await _userRepository.GetByIdAsync(book.AuthorId);
                if (author != null)
                {
                    var bookIndexDto = MapToIndexDto(book, author);
                    bookIndexDtos.Add(bookIndexDto);
                }
            }

            // Delete existing index and recreate
            await _searchService.DeleteIndexAsync(cancellationToken);
            await _searchService.CreateIndexAsync(cancellationToken);

            // Bulk index all books
            const int batchSize = 100;
            var batches = bookIndexDtos.Chunk(batchSize);

            foreach (var batch in batches)
            {
                var success = await _searchService.BulkIndexBooksAsync(batch, cancellationToken);
                if (!success)
                {
                    _logger.LogWarning("Failed to index batch of {Count} books", batch.Count());
                }
            }

            _logger.LogInformation("Completed reindexing {Count} books", bookIndexDtos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full reindex");
            throw;
        }
    }

    public async Task ReindexBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                _logger.LogWarning("Book {BookId} not found for reindexing", bookId);
                await _searchService.RemoveBookFromIndexAsync(bookId, cancellationToken);
                return;
            }

            if (!book.IsPublished)
            {
                _logger.LogDebug("Book {BookId} is not published, removing from index", bookId);
                await _searchService.RemoveBookFromIndexAsync(bookId, cancellationToken);
                return;
            }

            var author = await _userRepository.GetByIdAsync(book.AuthorId);
            if (author == null)
            {
                _logger.LogWarning("Author {AuthorId} not found for book {BookId}", book.AuthorId, bookId);
                return;
            }

            var bookIndexDto = MapToIndexDto(book, author);
            await _searchService.UpdateBookIndexAsync(bookId, bookIndexDto, cancellationToken);

            _logger.LogDebug("Successfully reindexed book {BookId}", bookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reindexing book {BookId}", bookId);
            throw;
        }
    }

    public async Task<bool> IsIndexHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthResponse = await _client.Cluster.HealthAsync(cancellationToken: cancellationToken);
            return healthResponse.IsValidResponse && 
                   (healthResponse.Status == Elastic.Clients.Elasticsearch.HealthStatus.Green || 
                    healthResponse.Status == Elastic.Clients.Elasticsearch.HealthStatus.Yellow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking index health");
            return false;
        }
    }

    public async Task<IndexStatistics> GetIndexStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var statsResponse = await _client.Indices.StatsAsync(_indexName, cancellationToken);
            
            if (!statsResponse.IsValidResponse)
            {
                _logger.LogWarning("Failed to get index statistics: {Error}", statsResponse.DebugInformation);
                return new IndexStatistics { Status = "Error" };
            }

            var indexStats = statsResponse.Indices?.FirstOrDefault().Value;
            
            return new IndexStatistics
            {
                TotalDocuments = indexStats?.Total?.Docs?.Count ?? 0,
                IndexSizeInBytes = indexStats?.Total?.Store?.SizeInBytes ?? 0,
                LastUpdated = DateTime.UtcNow,
                Status = "Healthy"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index statistics");
            return new IndexStatistics { Status = "Error" };
        }
    }

    private static BookIndexDto MapToIndexDto(Book book, User author)
    {
        return new BookIndexDto
        {
            Id = book.Id,
            Title = book.Title,
            Description = book.Description ?? string.Empty,
            Content = ExtractTextContent(book.ContentUrl), // This would extract text from the book content
            Author = new AuthorDto
            {
                Id = author.Id,
                Name = $"{author.FirstName} {author.LastName}".Trim(),
                IsVerified = author.IsEmailVerified
            },
            Category = book.Category ?? string.Empty,
            Tags = ExtractTags(book), // This would extract tags from book metadata
            Pricing = new PricingDto
            {
                PerPage = book.PricePerPage,
                PerHour = book.PricePerHour
            },
            Ratings = new RatingsDto
            {
                Average = CalculateAverageRating(book), // This would calculate from reviews
                Count = GetReviewCount(book) // This would count reviews
            },
            PublishedAt = book.PublishedAt ?? DateTime.UtcNow,
            Popularity = CalculatePopularity(book), // This would calculate based on views, purchases, etc.
            CoverImageUrl = book.CoverImageUrl ?? string.Empty,
            TotalPages = book.TotalPages,
            IsPublished = book.IsPublished,
            IsActive = true // Assuming active if it exists
        };
    }

    private static string ExtractTextContent(string? contentUrl)
    {
        // TODO: Implement content extraction from PDF or other formats
        // For now, return empty string
        return string.Empty;
    }

    private static List<string> ExtractTags(Book book)
    {
        // TODO: Implement tag extraction from book metadata or content
        // For now, return category as a tag
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(book.Category))
        {
            tags.Add(book.Category);
        }
        return tags;
    }

    private static float CalculateAverageRating(Book book)
    {
        // TODO: Implement rating calculation from reviews
        // For now, return a default rating
        return 4.0f;
    }

    private static int GetReviewCount(Book book)
    {
        // TODO: Implement review count from reviews
        // For now, return 0
        return 0;
    }

    private static float CalculatePopularity(Book book)
    {
        // TODO: Implement popularity calculation based on views, purchases, etc.
        // For now, return a default popularity score
        return 1.0f;
    }
}