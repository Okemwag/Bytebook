using ByteBook.Application.DTOs.Search;
using ByteBook.Application.Interfaces;
using ByteBook.Infrastructure.Search;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ByteBook.IntegrationTests.Infrastructure;

public class SearchServiceTests : IClassFixture<ElasticsearchTestFixture>
{
    private readonly ISearchService _searchService;
    private readonly ElasticsearchClient _client;

    public SearchServiceTests(ElasticsearchTestFixture fixture)
    {
        _searchService = fixture.ServiceProvider.GetRequiredService<ISearchService>();
        _client = fixture.ServiceProvider.GetRequiredService<ElasticsearchClient>();
    }

    [Fact]
    public async Task CreateIndex_Should_Create_Index_Successfully()
    {
        // Act
        var result = await _searchService.CreateIndexAsync();

        // Assert
        Assert.True(result);
        
        var exists = await _searchService.IndexExistsAsync();
        Assert.True(exists);
    }

    [Fact]
    public async Task IndexBook_Should_Index_Book_Successfully()
    {
        // Arrange
        await _searchService.CreateIndexAsync();
        
        var book = new BookIndexDto
        {
            Id = 1,
            Title = "Test Book",
            Description = "A test book for integration testing",
            Content = "This is the content of the test book for searching",
            Author = new AuthorDto { Id = 1, Name = "Test Author", IsVerified = true },
            Category = "Technology",
            Tags = new List<string> { "testing", "integration", "elasticsearch" },
            Pricing = new PricingDto { PerPage = 0.50m, PerHour = 5.00m },
            Ratings = new RatingsDto { Average = 4.5f, Count = 10 },
            PublishedAt = DateTime.UtcNow.AddDays(-30),
            Popularity = 0.8f,
            CoverImageUrl = "https://example.com/cover.jpg",
            TotalPages = 100,
            IsPublished = true,
            IsActive = true
        };

        // Act
        var result = await _searchService.IndexBookAsync(book);

        // Assert
        Assert.True(result);

        // Wait for indexing to complete
        await Task.Delay(1000);

        // Verify the book can be found
        var searchRequest = new BookSearchRequest
        {
            Query = "Test Book",
            PageSize = 10
        };

        var searchResult = await _searchService.SearchBooksAsync(searchRequest);
        Assert.True(searchResult.TotalCount > 0);
        Assert.Contains(searchResult.Items, b => b.Id == book.Id);
    }

    [Fact]
    public async Task SearchBooks_Should_Return_Relevant_Results()
    {
        // Arrange
        await _searchService.CreateIndexAsync();
        
        var books = new List<BookIndexDto>
        {
            new BookIndexDto
            {
                Id = 10,
                Title = "C# Programming Guide",
                Description = "Complete guide to C# programming",
                Content = "Learn C# programming from basics to advanced topics",
                Author = new AuthorDto { Id = 10, Name = "John Developer", IsVerified = true },
                Category = "Programming",
                Tags = new List<string> { "csharp", "programming", "dotnet" },
                Pricing = new PricingDto { PerPage = 0.75m, PerHour = 8.00m },
                Ratings = new RatingsDto { Average = 4.8f, Count = 25 },
                PublishedAt = DateTime.UtcNow.AddDays(-15),
                Popularity = 0.9f,
                TotalPages = 250,
                IsPublished = true,
                IsActive = true
            },
            new BookIndexDto
            {
                Id = 11,
                Title = "JavaScript Fundamentals",
                Description = "Learn JavaScript from scratch",
                Content = "JavaScript basics, DOM manipulation, and modern ES6+ features",
                Author = new AuthorDto { Id = 11, Name = "Jane Frontend", IsVerified = true },
                Category = "Programming",
                Tags = new List<string> { "javascript", "frontend", "web" },
                Pricing = new PricingDto { PerPage = 0.60m, PerHour = 6.50m },
                Ratings = new RatingsDto { Average = 4.3f, Count = 18 },
                PublishedAt = DateTime.UtcNow.AddDays(-10),
                Popularity = 0.7f,
                TotalPages = 180,
                IsPublished = true,
                IsActive = true
            },
            new BookIndexDto
            {
                Id = 12,
                Title = "Database Design Principles",
                Description = "Master database design and optimization",
                Content = "Relational database design, normalization, and performance tuning",
                Author = new AuthorDto { Id = 12, Name = "Bob Database", IsVerified = false },
                Category = "Database",
                Tags = new List<string> { "database", "sql", "design" },
                Pricing = new PricingDto { PerPage = 0.80m, PerHour = 9.00m },
                Ratings = new RatingsDto { Average = 4.6f, Count = 12 },
                PublishedAt = DateTime.UtcNow.AddDays(-5),
                Popularity = 0.6f,
                TotalPages = 200,
                IsPublished = true,
                IsActive = true
            }
        };

        // Index all books
        var bulkResult = await _searchService.BulkIndexBooksAsync(books);
        Assert.True(bulkResult);

        // Wait for indexing to complete
        await Task.Delay(2000);

        // Test 1: Search by title
        var titleSearchRequest = new BookSearchRequest
        {
            Query = "C# Programming",
            PageSize = 10
        };

        var titleSearchResult = await _searchService.SearchBooksAsync(titleSearchRequest);
        Assert.True(titleSearchResult.TotalCount > 0);
        Assert.Contains(titleSearchResult.Items, b => b.Id == 10);

        // Test 2: Search by category filter
        var categorySearchRequest = new BookSearchRequest
        {
            Categories = new List<string> { "Programming" },
            PageSize = 10
        };

        var categorySearchResult = await _searchService.SearchBooksAsync(categorySearchRequest);
        Assert.Equal(2, categorySearchResult.TotalCount);
        Assert.All(categorySearchResult.Items, b => Assert.Equal("Programming", b.Category));

        // Test 3: Search with price filter
        var priceSearchRequest = new BookSearchRequest
        {
            MinPricePerPage = 0.70m,
            MaxPricePerPage = 0.85m,
            PageSize = 10
        };

        var priceSearchResult = await _searchService.SearchBooksAsync(priceSearchRequest);
        Assert.True(priceSearchResult.TotalCount > 0);
        Assert.All(priceSearchResult.Items, b => 
        {
            Assert.True(b.Pricing.PerPage >= 0.70m);
            Assert.True(b.Pricing.PerPage <= 0.85m);
        });

        // Test 4: Search with rating filter
        var ratingSearchRequest = new BookSearchRequest
        {
            MinRating = 4.5f,
            PageSize = 10
        };

        var ratingSearchResult = await _searchService.SearchBooksAsync(ratingSearchRequest);
        Assert.True(ratingSearchResult.TotalCount > 0);
        Assert.All(ratingSearchResult.Items, b => Assert.True(b.Ratings.Average >= 4.5f));

        // Test 5: Search with sorting
        var sortedSearchRequest = new BookSearchRequest
        {
            SortBy = SearchSortBy.PublishedDate,
            SortOrder = SortOrder.Descending,
            PageSize = 10
        };

        var sortedSearchResult = await _searchService.SearchBooksAsync(sortedSearchRequest);
        Assert.True(sortedSearchResult.TotalCount > 0);
        
        // Verify results are sorted by published date (newest first)
        for (int i = 0; i < sortedSearchResult.Items.Count - 1; i++)
        {
            Assert.True(sortedSearchResult.Items[i].PublishedAt >= sortedSearchResult.Items[i + 1].PublishedAt);
        }
    }

    [Fact]
    public async Task GetSuggestions_Should_Return_Relevant_Suggestions()
    {
        // Arrange
        await _searchService.CreateIndexAsync();
        
        var books = new List<BookIndexDto>
        {
            new BookIndexDto
            {
                Id = 20,
                Title = "Programming with Python",
                IsPublished = true,
                IsActive = true
            },
            new BookIndexDto
            {
                Id = 21,
                Title = "Python Data Science",
                IsPublished = true,
                IsActive = true
            },
            new BookIndexDto
            {
                Id = 22,
                Title = "Advanced Python Techniques",
                IsPublished = true,
                IsActive = true
            }
        };

        await _searchService.BulkIndexBooksAsync(books);
        await Task.Delay(2000);

        // Act
        var suggestions = await _searchService.GetSuggestionsAsync("Prog", 5);

        // Assert
        Assert.NotEmpty(suggestions);
        // Note: Suggestions might not work perfectly in test environment without proper completion mapping
    }

    [Fact]
    public async Task UpdateBookIndex_Should_Update_Existing_Book()
    {
        // Arrange
        await _searchService.CreateIndexAsync();
        
        var originalBook = new BookIndexDto
        {
            Id = 30,
            Title = "Original Title",
            Description = "Original description",
            IsPublished = true,
            IsActive = true
        };

        await _searchService.IndexBookAsync(originalBook);
        await Task.Delay(1000);

        var updatedBook = new BookIndexDto
        {
            Id = 30,
            Title = "Updated Title",
            Description = "Updated description",
            IsPublished = true,
            IsActive = true
        };

        // Act
        var updateResult = await _searchService.UpdateBookIndexAsync(30, updatedBook);
        Assert.True(updateResult);

        await Task.Delay(1000);

        // Assert
        var searchRequest = new BookSearchRequest
        {
            Query = "Updated Title",
            PageSize = 10
        };

        var searchResult = await _searchService.SearchBooksAsync(searchRequest);
        Assert.True(searchResult.TotalCount > 0);
        
        var foundBook = searchResult.Items.FirstOrDefault(b => b.Id == 30);
        Assert.NotNull(foundBook);
        Assert.Equal("Updated Title", foundBook.Title);
        Assert.Equal("Updated description", foundBook.Description);
    }

    [Fact]
    public async Task RemoveBookFromIndex_Should_Remove_Book_Successfully()
    {
        // Arrange
        await _searchService.CreateIndexAsync();
        
        var book = new BookIndexDto
        {
            Id = 40,
            Title = "Book to Remove",
            Description = "This book will be removed",
            IsPublished = true,
            IsActive = true
        };

        await _searchService.IndexBookAsync(book);
        await Task.Delay(1000);

        // Verify book exists
        var searchRequest = new BookSearchRequest
        {
            Query = "Book to Remove",
            PageSize = 10
        };

        var searchResult = await _searchService.SearchBooksAsync(searchRequest);
        Assert.True(searchResult.TotalCount > 0);

        // Act
        var removeResult = await _searchService.RemoveBookFromIndexAsync(40);
        Assert.True(removeResult);

        await Task.Delay(1000);

        // Assert
        var searchAfterRemoval = await _searchService.SearchBooksAsync(searchRequest);
        Assert.DoesNotContain(searchAfterRemoval.Items, b => b.Id == 40);
    }
}

public class ElasticsearchTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    private readonly ElasticsearchClient _client;

    public ElasticsearchTestFixture()
    {
        var services = new ServiceCollection();
        
        // Configure Elasticsearch for testing
        var elasticsearchUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URI") ?? "http://localhost:9200";
        
        services.AddSingleton<ElasticsearchClient>(provider =>
        {
            var uri = new Uri(elasticsearchUri);
            var settings = new ElasticsearchClientSettings(uri)
                .RequestTimeout(TimeSpan.FromSeconds(30))
                .MaximumRetries(3)
                .ThrowExceptions(false);

            return new ElasticsearchClient(settings);
        });

        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<ISearchService, ElasticsearchService>();

        ServiceProvider = services.BuildServiceProvider();
        _client = ServiceProvider.GetRequiredService<ElasticsearchClient>();
    }

    public void Dispose()
    {
        // Clean up test index
        try
        {
            _client.Indices.DeleteAsync("bytebook-books").GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore cleanup errors
        }

        (ServiceProvider as IDisposable)?.Dispose();
    }
}