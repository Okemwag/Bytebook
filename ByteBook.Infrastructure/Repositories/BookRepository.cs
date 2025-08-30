using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Repositories;

public class BookRepository : Repository<Book>, IBookRepository
{
    public BookRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Book>> GetByAuthorAsync(int authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.AuthorId == authorId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetPublishedBooksAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.IsPublished && b.IsActive)
            .OrderByDescending(b => b.PublishedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetByCategoryAsync(string category, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Category == category && b.IsPublished && b.IsActive)
            .OrderByDescending(b => b.PublishedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> SearchAsync(string query, string? category = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var dbQuery = _dbSet
            .Where(b => b.IsPublished && b.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            dbQuery = dbQuery.Where(b => 
                b.Title.ToLower().Contains(lowerQuery) ||
                b.Description.ToLower().Contains(lowerQuery) ||
                (b.Tags != null && b.Tags.ToLower().Contains(lowerQuery)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            dbQuery = dbQuery.Where(b => b.Category == category);
        }

        return await dbQuery
            .OrderByDescending(b => b.AverageRating)
            .ThenByDescending(b => b.PublishedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book?> GetWithAuthorAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => EF.Property<User>(b, "Author"))
            .FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetTrendingBooksAsync(int days = 7, int take = 10, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        // This is a simplified trending algorithm - in a real implementation,
        // you'd want to consider reading activity, payments, etc.
        return await _dbSet
            .Where(b => b.IsPublished && b.IsActive && b.PublishedAt >= cutoffDate)
            .OrderByDescending(b => b.AverageRating)
            .ThenByDescending(b => b.ReviewCount)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetRecentlyPublishedAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.IsPublished && b.IsActive)
            .OrderByDescending(b => b.PublishedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.IsPublished && b.IsActive)
            .Select(b => b.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TitleExistsForAuthorAsync(string title, int authorId, int? excludeBookId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(b => b.Title == title && b.AuthorId == authorId);

        if (excludeBookId.HasValue)
        {
            query = query.Where(b => b.Id != excludeBookId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}