using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Repositories;

public class ReadingRepository : Repository<Reading>, IReadingRepository
{
    public ReadingRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Reading>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reading>> GetByBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.BookId == bookId)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reading?> GetActiveSessionAsync(int userId, int bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.UserId == userId && 
                                     r.BookId == bookId && 
                                     r.Status == ReadingStatus.Active, cancellationToken);
    }

    public async Task<IEnumerable<Reading>> GetActiveSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId && r.Status == ReadingStatus.Active)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reading>> GetCompletedReadingsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId && r.IsCompleted)
            .OrderByDescending(r => r.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Reading?> GetLatestReadingAsync(int userId, int bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .OrderByDescending(r => r.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Reading>> GetReadingHistoryAsync(int userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalPagesReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .SumAsync(r => r.PagesRead, cancellationToken);
    }

    public async Task<int> GetTotalReadingTimeAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserId == userId)
            .SumAsync(r => r.TimeSpentMinutes, cancellationToken);
    }

    public async Task<IEnumerable<Reading>> GetLongRunningSessions(int maxHours = 24, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-maxHours);
        
        return await _dbSet
            .Where(r => r.Status == ReadingStatus.Active && r.StartTime <= cutoffTime)
            .OrderBy(r => r.StartTime)
            .ToListAsync(cancellationToken);
    }
}