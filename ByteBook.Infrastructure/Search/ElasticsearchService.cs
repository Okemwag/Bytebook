using ByteBook.Application.DTOs.Search;
using ByteBook.Application.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ByteBook.Infrastructure.Search;

public class ElasticsearchService : ISearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName = "bytebook-books";

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SearchResult<BookSearchDto>> SearchBooksAsync(BookSearchRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var searchRequest = BuildSearchRequest(request);
            var response = await _client.SearchAsync<BookIndexDto>(searchRequest, cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                return new SearchResult<BookSearchDto>();
            }

            var result = new SearchResult<BookSearchDto>
            {
                Items = response.Documents.Select(MapToSearchDto).ToList(),
                TotalCount = response.Total,
                Page = request.Page,
                PageSize = request.PageSize,
                SearchTime = stopwatch.Elapsed,
                Facets = ExtractFacets(response),
                Suggestions = await GetSuggestionsAsync(request.Query, 5, cancellationToken)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing book search");
            return new SearchResult<BookSearchDto>();
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public async Task<bool> IndexBookAsync(BookIndexDto book, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.IndexAsync(book, _indexName, book.Id.ToString(), cancellationToken);
            
            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to index book {BookId}: {Error}", book.Id, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully indexed book {BookId}", book.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing book {BookId}", book.Id);
            return false;
        }
    }

    public async Task<bool> UpdateBookIndexAsync(int bookId, BookIndexDto book, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.UpdateAsync<BookIndexDto, BookIndexDto>(_indexName, bookId.ToString(), u => u
                .Doc(book)
                .DocAsUpsert(true), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to update book index {BookId}: {Error}", bookId, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully updated book index {BookId}", bookId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book index {BookId}", bookId);
            return false;
        }
    }

    public async Task<bool> RemoveBookFromIndexAsync(int bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync(_indexName, bookId.ToString(), cancellationToken);
            
            if (!response.IsValidResponse && response.Result != Result.NotFound)
            {
                _logger.LogError("Failed to remove book {BookId} from index: {Error}", bookId, response.DebugInformation);
                return false;
            }

            _logger.LogDebug("Successfully removed book {BookId} from index", bookId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing book {BookId} from index", bookId);
            return false;
        }
    }

    public async Task<bool> BulkIndexBooksAsync(IEnumerable<BookIndexDto> books, CancellationToken cancellationToken = default)
    {
        try
        {
            var bulkRequest = new BulkRequest(_indexName)
            {
                Operations = new BulkOperationsCollection(
                    books.Select(book => new BulkIndexOperation<BookIndexDto>(book)
                    {
                        Id = book.Id.ToString()
                    }).Cast<IBulkOperation>()
                )
            };

            var response = await _client.BulkAsync(bulkRequest, cancellationToken);

            if (response.Errors)
            {
                _logger.LogError("Bulk indexing had errors: {Errors}", 
                    string.Join(", ", response.Items.Where(i => i.Error != null).Select(i => i.Error!.Reason)));
                return false;
            }

            _logger.LogInformation("Successfully bulk indexed {Count} books", books.Count());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing books");
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetSuggestionsAsync(string query, int maxSuggestions = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<string>();

        try
        {
            var searchRequest = new SearchRequest(_indexName)
            {
                Size = 0,
                Suggest = new Dictionary<string, ISuggestContainer>
                {
                    ["title_suggest"] = new CompletionSuggester
                    {
                        Field = "title.suggest",
                        Prefix = query,
                        Size = maxSuggestions
                    }
                }
            };

            var response = await _client.SearchAsync<BookIndexDto>(searchRequest, cancellationToken);

            if (!response.IsValidResponse || response.Suggest == null)
                return Enumerable.Empty<string>();

            var suggestions = new List<string>();
            if (response.Suggest.TryGetValue("title_suggest", out var titleSuggestions))
            {
                foreach (var suggestion in titleSuggestions)
                {
                    if (suggestion is CompletionSuggest completionSuggest)
                    {
                        suggestions.AddRange(completionSuggest.Options.Select(o => o.Text));
                    }
                }
            }

            return suggestions.Distinct().Take(maxSuggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        // This would typically query a separate analytics index
        // For now, return empty analytics
        return new SearchAnalyticsDto();
    }

    public async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var indexExists = await IndexExistsAsync(cancellationToken);
            if (indexExists)
            {
                _logger.LogInformation("Index {IndexName} already exists", _indexName);
                return true;
            }

            var createIndexRequest = new CreateIndexRequest(_indexName)
            {
                Mappings = new TypeMapping
                {
                    Properties = new Properties
                    {
                        ["id"] = new IntegerNumberProperty(),
                        ["title"] = new TextProperty
                        {
                            Analyzer = "standard",
                            Fields = new Properties
                            {
                                ["keyword"] = new KeywordProperty(),
                                ["suggest"] = new CompletionProperty()
                            }
                        },
                        ["description"] = new TextProperty { Analyzer = "standard" },
                        ["content"] = new TextProperty { Analyzer = "standard" },
                        ["author"] = new ObjectProperty
                        {
                            Properties = new Properties
                            {
                                ["id"] = new IntegerNumberProperty(),
                                ["name"] = new TextProperty
                                {
                                    Fields = new Properties
                                    {
                                        ["keyword"] = new KeywordProperty()
                                    }
                                },
                                ["isVerified"] = new BooleanProperty()
                            }
                        },
                        ["category"] = new KeywordProperty(),
                        ["tags"] = new KeywordProperty(),
                        ["pricing"] = new ObjectProperty
                        {
                            Properties = new Properties
                            {
                                ["perPage"] = new FloatNumberProperty(),
                                ["perHour"] = new FloatNumberProperty()
                            }
                        },
                        ["ratings"] = new ObjectProperty
                        {
                            Properties = new Properties
                            {
                                ["average"] = new FloatNumberProperty(),
                                ["count"] = new IntegerNumberProperty()
                            }
                        },
                        ["publishedAt"] = new DateProperty(),
                        ["popularity"] = new FloatNumberProperty(),
                        ["coverImageUrl"] = new KeywordProperty(),
                        ["totalPages"] = new IntegerNumberProperty(),
                        ["isPublished"] = new BooleanProperty(),
                        ["isActive"] = new BooleanProperty()
                    }
                }
            };

            var response = await _client.Indices.CreateAsync(createIndexRequest, cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", _indexName, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully created index {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", _indexName);
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.DeleteAsync(_indexName, cancellationToken);
            
            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}", _indexName, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully deleted index {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting index {IndexName}", _indexName);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.Indices.ExistsAsync(_indexName, cancellationToken);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index exists {IndexName}", _indexName);
            return false;
        }
    }

    private SearchRequest BuildSearchRequest(BookSearchRequest request)
    {
        var queries = new List<Query>();

        // Main query
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var multiMatchQuery = new MultiMatchQuery
            {
                Query = request.Query,
                Fields = new[] { "title^3", "description^2", "content", "author.name^2", "tags" },
                Type = TextQueryType.BestFields,
                Fuzziness = new Fuzziness("AUTO")
            };
            queries.Add(multiMatchQuery);
        }

        // Filters
        var filters = new List<Query>();

        if (request.Categories.Any())
        {
            filters.Add(new TermsQuery { Field = "category", Terms = request.Categories.Select(c => FieldValue.String(c)) });
        }

        if (request.Tags.Any())
        {
            filters.Add(new TermsQuery { Field = "tags", Terms = request.Tags.Select(t => FieldValue.String(t)) });
        }

        if (request.AuthorId.HasValue)
        {
            filters.Add(new TermQuery("author.id") { Value = request.AuthorId.Value });
        }

        if (request.MinRating.HasValue)
        {
            filters.Add(new RangeQuery("ratings.average") { Gte = request.MinRating.Value });
        }

        // Price filters
        if (request.MinPricePerPage.HasValue || request.MaxPricePerPage.HasValue)
        {
            var priceQuery = new RangeQuery("pricing.perPage");
            if (request.MinPricePerPage.HasValue) priceQuery.Gte = (double)request.MinPricePerPage.Value;
            if (request.MaxPricePerPage.HasValue) priceQuery.Lte = (double)request.MaxPricePerPage.Value;
            filters.Add(priceQuery);
        }

        if (request.MinPricePerHour.HasValue || request.MaxPricePerHour.HasValue)
        {
            var priceQuery = new RangeQuery("pricing.perHour");
            if (request.MinPricePerHour.HasValue) priceQuery.Gte = (double)request.MinPricePerHour.Value;
            if (request.MaxPricePerHour.HasValue) priceQuery.Lte = (double)request.MaxPricePerHour.Value;
            filters.Add(priceQuery);
        }

        // Date filters
        if (request.PublishedAfter.HasValue || request.PublishedBefore.HasValue)
        {
            var dateQuery = new RangeQuery("publishedAt");
            if (request.PublishedAfter.HasValue) dateQuery.Gte = DateMath.FromDateTime(request.PublishedAfter.Value);
            if (request.PublishedBefore.HasValue) dateQuery.Lte = DateMath.FromDateTime(request.PublishedBefore.Value);
            filters.Add(dateQuery);
        }

        // Always filter for published and active books
        filters.Add(new TermQuery("isPublished") { Value = true });
        filters.Add(new TermQuery("isActive") { Value = true });

        // Build final query
        Query finalQuery;
        if (queries.Any() && filters.Any())
        {
            finalQuery = new BoolQuery
            {
                Must = queries,
                Filter = filters
            };
        }
        else if (queries.Any())
        {
            finalQuery = new BoolQuery { Must = queries };
        }
        else if (filters.Any())
        {
            finalQuery = new BoolQuery { Filter = filters };
        }
        else
        {
            finalQuery = new MatchAllQuery();
        }

        // Build sort
        var sorts = new List<SortOptions>();
        switch (request.SortBy)
        {
            case SearchSortBy.Relevance:
                sorts.Add(SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }));
                break;
            case SearchSortBy.PublishedDate:
                sorts.Add(SortOptions.Field("publishedAt", new FieldSort { Order = request.SortOrder == SortOrder.Descending ? Elastic.Clients.Elasticsearch.SortOrder.Desc : Elastic.Clients.Elasticsearch.SortOrder.Asc }));
                break;
            case SearchSortBy.Rating:
                sorts.Add(SortOptions.Field("ratings.average", new FieldSort { Order = request.SortOrder == SortOrder.Descending ? Elastic.Clients.Elasticsearch.SortOrder.Desc : Elastic.Clients.Elasticsearch.SortOrder.Asc }));
                break;
            case SearchSortBy.Popularity:
                sorts.Add(SortOptions.Field("popularity", new FieldSort { Order = request.SortOrder == SortOrder.Descending ? Elastic.Clients.Elasticsearch.SortOrder.Desc : Elastic.Clients.Elasticsearch.SortOrder.Asc }));
                break;
            case SearchSortBy.Title:
                sorts.Add(SortOptions.Field("title.keyword", new FieldSort { Order = request.SortOrder == SortOrder.Descending ? Elastic.Clients.Elasticsearch.SortOrder.Desc : Elastic.Clients.Elasticsearch.SortOrder.Asc }));
                break;
        }

        return new SearchRequest(_indexName)
        {
            Query = finalQuery,
            Sort = sorts,
            From = (request.Page - 1) * request.PageSize,
            Size = request.PageSize,
            Aggregations = new Dictionary<string, Aggregation>
            {
                ["categories"] = new TermsAggregation("category") { Size = 20 },
                ["tags"] = new TermsAggregation("tags") { Size = 50 },
                ["authors"] = new TermsAggregation("author.name.keyword") { Size = 20 }
            }
        };
    }

    private static BookSearchDto MapToSearchDto(BookIndexDto source)
    {
        return new BookSearchDto
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Author = source.Author,
            Category = source.Category,
            Tags = source.Tags,
            Pricing = source.Pricing,
            Ratings = source.Ratings,
            PublishedAt = source.PublishedAt,
            Popularity = source.Popularity,
            CoverImageUrl = source.CoverImageUrl,
            TotalPages = source.TotalPages
        };
    }

    private static List<SearchFacet> ExtractFacets(SearchResponse<BookIndexDto> response)
    {
        var facets = new List<SearchFacet>();

        if (response.Aggregations == null)
            return facets;

        foreach (var (key, aggregation) in response.Aggregations)
        {
            if (aggregation is StringTermsAggregate termsAgg)
            {
                var facet = new SearchFacet
                {
                    Name = key,
                    Values = termsAgg.Buckets.Select(b => new FacetValue
                    {
                        Value = b.Key.ToString(),
                        Count = b.DocCount ?? 0
                    }).ToList()
                };
                facets.Add(facet);
            }
        }

        return facets;
    }
}