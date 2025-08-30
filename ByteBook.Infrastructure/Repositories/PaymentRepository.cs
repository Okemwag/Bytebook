using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Payment>> GetByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByBookAsync(int bookId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.BookId == bookId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByAuthorAsync(int authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Join(_context.Books, p => p.BookId, b => b.Id, (p, b) => new { Payment = p, Book = b })
            .Where(pb => pb.Book.AuthorId == authorId)
            .Select(pb => pb.Payment)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ExternalTransactionId == externalTransactionId, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetCompletedPaymentsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(p => p.Status == PaymentStatus.Completed);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(p => p.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetFailedPaymentsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(p => p.Status == PaymentStatus.Failed);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Money> GetTotalEarningsForAuthorAsync(int authorId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Join(_context.Books, p => p.BookId, b => b.Id, (p, b) => new { Payment = p, Book = b })
            .Where(pb => pb.Book.AuthorId == authorId && pb.Payment.Status == PaymentStatus.Completed);

        if (fromDate.HasValue)
        {
            query = query.Where(pb => pb.Payment.ProcessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(pb => pb.Payment.ProcessedAt <= toDate.Value);
        }

        var payments = await query
            .Select(pb => pb.Payment)
            .ToListAsync(cancellationToken);

        if (!payments.Any())
        {
            return Money.Zero();
        }

        // Calculate total earnings with platform commission deducted
        var totalEarnings = Money.Zero(payments.First().Amount.Currency);
        foreach (var payment in payments)
        {
            var earnings = payment.CalculateAuthorEarnings();
            totalEarnings = totalEarnings.Add(earnings);
        }

        return totalEarnings;
    }

    public async Task<Money> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(p => p.Status == PaymentStatus.Completed);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.ProcessedAt <= toDate.Value);
        }

        var payments = await query.ToListAsync(cancellationToken);

        if (!payments.Any())
        {
            return Money.Zero();
        }

        var totalRevenue = Money.Zero(payments.First().Amount.Currency);
        foreach (var payment in payments)
        {
            var netAmount = payment.RefundedAmount == null ? 
                payment.Amount : 
                payment.Amount.Subtract(payment.RefundedAmount);
            totalRevenue = totalRevenue.Add(netAmount);
        }

        return totalRevenue;
    }

    public async Task<IEnumerable<Payment>> GetRefundablePaymentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.Status == PaymentStatus.Completed)
            .Where(p => p.RefundedAmount == null || p.RefundedAmount.Amount < p.Amount.Amount)
            .OrderByDescending(p => p.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByReadingSessionAsync(int readingSessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(p => p.ReadingSessionId == readingSessionId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}