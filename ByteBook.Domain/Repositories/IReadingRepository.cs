using ByteBook.Domain.Entities;

namespace ByteBook.Domain.Repositories;

public interface IReadingRepository : IRepository<Reading>
{
    Task<IEnumerable<Reading>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetByBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task<Reading?> GetActiveSessionAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetActiveSessionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetCompletedReadingsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Reading?> GetLatestReadingAsync(int userId, int bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetReadingHistoryAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetTotalPagesReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetTotalReadingTimeAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Reading>> GetLongRunningSessions(int maxHours = 24, CancellationToken cancellationToken = default);
}