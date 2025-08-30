using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Repositories;

public interface IReferralRepository : IRepository<Referral>
{
    Task<IEnumerable<Referral>> GetByReferrerAsync(int referrerId, CancellationToken cancellationToken = default);
    Task<Referral?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default);
    Task<Referral?> GetByReferredUserAsync(int referredUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Referral>> GetPendingReferralsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Referral>> GetConvertedReferralsAsync(int referrerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Referral>> GetExpiredReferralsAsync(TimeSpan expirationPeriod, CancellationToken cancellationToken = default);
    Task<Money> GetTotalCommissionEarnedAsync(int referrerId, CancellationToken cancellationToken = default);
    Task<Money> GetUnpaidCommissionAsync(int referrerId, CancellationToken cancellationToken = default);
    Task<bool> ReferralCodeExistsAsync(string referralCode, CancellationToken cancellationToken = default);
    Task<int> GetConversionCountAsync(int referrerId, CancellationToken cancellationToken = default);
}