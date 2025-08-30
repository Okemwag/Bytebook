using Microsoft.EntityFrameworkCore;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Repositories;

public class ReferralRepository : Repository<Referral>, IReferralRepository
{
    public ReferralRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Referral>> GetByReferrerAsync(int referrerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReferrerId == referrerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Referral?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.ReferralCode == referralCode.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Referral?> GetByReferredUserAsync(int referredUserId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.ReferredUserId == referredUserId, cancellationToken);
    }

    public async Task<IEnumerable<Referral>> GetPendingReferralsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == ReferralStatus.Pending && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Referral>> GetConvertedReferralsAsync(int referrerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReferrerId == referrerId && 
                       (r.Status == ReferralStatus.Converted || 
                        r.Status == ReferralStatus.CommissionCalculated || 
                        r.Status == ReferralStatus.CommissionPaid))
            .OrderByDescending(r => r.ConvertedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Referral>> GetExpiredReferralsAsync(TimeSpan expirationPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow - expirationPeriod;
        
        return await _dbSet
            .Where(r => r.Status == ReferralStatus.Pending && 
                       r.CreatedAt <= cutoffDate && 
                       r.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Money> GetTotalCommissionEarnedAsync(int referrerId, CancellationToken cancellationToken = default)
    {
        var referrals = await _dbSet
            .Where(r => r.ReferrerId == referrerId && 
                       r.CommissionEarned != null &&
                       (r.Status == ReferralStatus.CommissionCalculated || 
                        r.Status == ReferralStatus.CommissionPaid))
            .ToListAsync(cancellationToken);

        if (!referrals.Any())
        {
            return Money.Zero();
        }

        var totalCommission = Money.Zero(referrals.First().CommissionEarned!.Currency);
        foreach (var referral in referrals)
        {
            totalCommission = totalCommission.Add(referral.CommissionEarned!);
        }

        return totalCommission;
    }

    public async Task<Money> GetUnpaidCommissionAsync(int referrerId, CancellationToken cancellationToken = default)
    {
        var referrals = await _dbSet
            .Where(r => r.ReferrerId == referrerId && 
                       r.CommissionEarned != null &&
                       r.Status == ReferralStatus.CommissionCalculated)
            .ToListAsync(cancellationToken);

        if (!referrals.Any())
        {
            return Money.Zero();
        }

        var unpaidCommission = Money.Zero(referrals.First().CommissionEarned!.Currency);
        foreach (var referral in referrals)
        {
            unpaidCommission = unpaidCommission.Add(referral.CommissionEarned!);
        }

        return unpaidCommission;
    }

    public async Task<bool> ReferralCodeExistsAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(r => r.ReferralCode == referralCode.ToUpperInvariant(), cancellationToken);
    }

    public async Task<int> GetConversionCountAsync(int referrerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(r => r.ReferrerId == referrerId && 
                            (r.Status == ReferralStatus.Converted || 
                             r.Status == ReferralStatus.CommissionCalculated || 
                             r.Status == ReferralStatus.CommissionPaid), cancellationToken);
    }
}