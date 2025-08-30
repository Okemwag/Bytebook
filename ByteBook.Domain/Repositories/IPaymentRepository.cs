using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByBookAsync(int bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByAuthorAsync(int authorId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByExternalTransactionIdAsync(string externalTransactionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetCompletedPaymentsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetFailedPaymentsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Money> GetTotalEarningsForAuthorAsync(int authorId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Money> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetRefundablePaymentsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByReadingSessionAsync(int readingSessionId, CancellationToken cancellationToken = default);
}