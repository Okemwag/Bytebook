# ByteBook Caching and Search Implementation

This document describes the Redis caching and Elasticsearch search implementation for the ByteBook platform.

## Overview

The caching and search system provides:
- **Redis-based distributed caching** for performance optimization
- **Elasticsearch-powered search** for content discovery
- **Distributed locking** to prevent cache stampede
- **Session management** for user state
- **Caching strategies** with common patterns

## Architecture

### Redis Caching Layer

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Application   │───▶│  Cache Services  │───▶│      Redis      │
│     Layer       │    │                  │    │    Cluster     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────────┐
                       │ Caching Strategy │
                       │   & Patterns     │
                       └──────────────────┘
```

### Elasticsearch Search Layer

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Search API    │───▶│ Search Services  │───▶│ Elasticsearch   │
│   Controllers   │    │                  │    │    Cluster     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────────┐
                       │ Index Management │
                       │   & Analytics    │
                       └──────────────────┘
```

## Cache Services

### ICacheService
Core caching interface providing:
- `GetAsync<T>` / `SetAsync<T>` - Basic get/set operations
- `GetManyAsync<T>` / `SetManyAsync<T>` - Bulk operations
- `IncrementAsync` / `DecrementAsync` - Atomic counters
- `ExistsAsync` / `RemoveAsync` - Key management
- `RemoveByPatternAsync` - Pattern-based cleanup

### IDistributedLockService
Distributed locking to prevent race conditions:
- `AcquireLockAsync` - Acquire exclusive lock
- `ExtendAsync` - Extend lock duration
- `ReleaseAsync` - Release lock explicitly

### ISessionCacheService
User session management:
- `GetSessionDataAsync<T>` / `SetSessionDataAsync<T>` - Session data
- `ExtendSessionAsync` - Extend session timeout
- `ClearSessionAsync` - Clear entire session

## Caching Strategies

### ICachingStrategy
High-level caching patterns:
- **Cache-Aside Pattern**: `GetOrSetAsync` with factory function
- **Cache Stampede Prevention**: Distributed locking
- **Conditional Caching**: Custom shouldCache predicate
- **Cache Warming**: `WarmupAsync` for preloading
- **Pattern-based Invalidation**: Bulk cache cleanup

### Specialized Caching Services

#### BookCachingService
- Book metadata caching
- Page content caching
- Author-specific invalidation

#### UserCachingService
- User profile caching
- Email-based lookups
- User-specific invalidation

#### SearchCachingService
- Search result caching
- Query-based cache keys
- Search invalidation patterns

## Cache Key Patterns

### Naming Convention
```
{entity}:{id}:{subtype}:{additional_params}
```

### Examples
```csharp
// User keys
user:123                    // User profile
user:email:john@example.com // User by email
user:123:sessions          // User sessions

// Book keys
book:456                   // Book metadata
book:456:content:10        // Page 10 content
books:author:789           // Books by author
books:category:Technology  // Books in category

// Search keys
search:results:abc123      // Search results hash
search:suggestions:prog    // Search suggestions

// System keys
ratelimit:user123:login    // Rate limiting
blacklist:token:xyz789     // Token blacklist
```

## Search Services

### ISearchService
Core search functionality:
- `SearchBooksAsync` - Full-text search with filters
- `IndexBookAsync` / `UpdateBookIndexAsync` - Document indexing
- `GetSuggestionsAsync` - Auto-complete suggestions
- `BulkIndexBooksAsync` - Bulk document operations

### ISearchIndexService
Index management:
- `ReindexAllBooksAsync` - Full reindex operation
- `ReindexBookAsync` - Single book reindex
- `IsIndexHealthyAsync` - Health monitoring
- `GetIndexStatisticsAsync` - Index metrics

## Configuration

### Redis Configuration
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "InstanceName": "ByteBook",
    "DefaultExpiry": "01:00:00",
    "AbortOnConnectFail": false,
    "ConnectRetry": 3,
    "ConnectTimeout": "00:00:05",
    "SyncTimeout": "00:00:05"
  }
}
```

### Elasticsearch Configuration
```json
{
  "Elasticsearch": {
    "Uri": "http://localhost:9200",
    "Username": "",
    "Password": "",
    "IndexPrefix": "bytebook",
    "EnableDebugMode": false,
    "RequestTimeout": "00:00:30",
    "MaxRetries": 3,
    "ThrowExceptions": false
  }
}
```

### Cache Settings
```json
{
  "Cache": {
    "DefaultExpiry": "00:30:00",
    "UserSessionExpiry": "1.00:00:00",
    "BookContentExpiry": "06:00:00",
    "SearchResultsExpiry": "00:15:00",
    "TokenBlacklistExpiry": "7.00:00:00",
    "EnableDistributedLocking": true,
    "LockTimeout": "00:00:30",
    "LockExpiry": "00:05:00"
  }
}
```

## Usage Examples

### Basic Caching
```csharp
// Simple cache-aside pattern
var user = await _cachingStrategy.GetOrSetAsync(
    CacheKeys.User(userId),
    () => _userRepository.GetByIdAsync(userId),
    TimeSpan.FromMinutes(30)
);

// Conditional caching
var books = await _cachingStrategy.GetOrSetAsync(
    CacheKeys.BooksByAuthor(authorId),
    () => _bookRepository.GetByAuthorAsync(authorId),
    result => result.Any(), // Only cache if books found
    TimeSpan.FromHours(1)
);
```

### Search Operations
```csharp
// Search books with filters
var searchRequest = new BookSearchRequest
{
    Query = "C# programming",
    Categories = new[] { "Technology", "Programming" },
    MinRating = 4.0f,
    SortBy = SearchSortBy.Relevance,
    Page = 1,
    PageSize = 20
};

var results = await _searchService.SearchBooksAsync(searchRequest);

// Index a book
var bookIndex = new BookIndexDto
{
    Id = book.Id,
    Title = book.Title,
    Content = ExtractContent(book),
    // ... other properties
};

await _searchService.IndexBookAsync(bookIndex);
```

### Session Management
```csharp
// Store user session data
await _sessionCacheService.SetSessionDataAsync(
    sessionId,
    "reading_progress",
    new ReadingProgress { BookId = 123, PageNumber = 45 },
    TimeSpan.FromHours(24)
);

// Retrieve session data
var progress = await _sessionCacheService.GetSessionDataAsync<ReadingProgress>(
    sessionId,
    "reading_progress"
);
```

### Distributed Locking
```csharp
// Prevent concurrent operations
using var lockHandle = await _lockService.AcquireLockAsync(
    $"process_payment:{paymentId}",
    TimeSpan.FromMinutes(5),
    TimeSpan.FromSeconds(30)
);

if (lockHandle != null)
{
    // Perform critical operation
    await ProcessPaymentAsync(paymentId);
}
```

## Performance Considerations

### Cache Expiry Strategy
- **User Data**: 30 minutes (frequent updates)
- **Book Content**: 6 hours (static content)
- **Search Results**: 15 minutes (dynamic results)
- **Session Data**: 24 hours (user activity)

### Search Optimization
- **Index Warming**: Preload popular content
- **Result Caching**: Cache frequent queries
- **Bulk Operations**: Batch index updates
- **Faceted Search**: Enable category filtering

### Monitoring
- Cache hit/miss ratios
- Search response times
- Index health status
- Lock contention metrics

## Testing

### Integration Tests
- Redis connectivity and operations
- Elasticsearch indexing and search
- Distributed locking behavior
- Session management lifecycle

### Unit Tests
- Caching strategy patterns
- Cache key generation
- Search query building
- Error handling scenarios

## Deployment

### Docker Compose
Services are configured in `docker-compose.yml`:
- Redis: Port 6379
- Elasticsearch: Port 9200
- Health checks enabled
- Persistent volumes configured

### Production Considerations
- Redis clustering for high availability
- Elasticsearch cluster configuration
- Monitoring and alerting setup
- Backup and recovery procedures