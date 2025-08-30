using ByteBook.Domain.Entities;

namespace ByteBook.Domain.Repositories;

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetByAuthorAsync(int authorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetPublishedBooksAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetByCategoryAsync(string category, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> SearchAsync(string query, string? category = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<Book?> GetWithAuthorAsync(int bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetTrendingBooksAsync(int days = 7, int take = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetRecentlyPublishedAsync(int take = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<bool> TitleExistsForAuthorAsync(string title, int authorId, int? excludeBookId = null, CancellationToken cancellationToken = default);
}