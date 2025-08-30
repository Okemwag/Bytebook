using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Entities;

public class Referral : BaseEntity
{
    public int ReferrerId { get; private set; }
    public int ReferredUserId { get; private set; }
    public string ReferralCode { get; private set; }
    public ReferralStatus Status { get; private set; }
    public DateTime? ConvertedAt { get; private set; }
    public Money? CommissionEarned { get; private set; }
    public decimal CommissionRate { get; private set; }
    public ReferralType Type { get; private set; }
    public int? TriggerPaymentId { get; private set; }
    public Money? TriggerAmount { get; private set; }
    public DateTime? CommissionPaidAt { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties - will be uncommented when entities are created
    // public User Referrer { get; private set; }
    // public User ReferredUser { get; private set; }
    // public Payment? TriggerPayment { get; private set; }

    private Referral()
    {
        ReferralCode = string.Empty;
    } // EF Core

    public Referral(int referrerId, string referralCode, ReferralType type, decimal commissionRate = 0.10m)
    {
        if (referrerId <= 0)
            throw new ArgumentException("Referrer ID must be positive", nameof(referrerId));
        
        if (string.IsNullOrWhiteSpace(referralCode))
            throw new ArgumentException("Referral code cannot be empty", nameof(referralCode));
        
        if (commissionRate < 0 || commissionRate > 1)
            throw new ArgumentException("Commission rate must be between 0 and 1", nameof(commissionRate));

        ReferrerId = referrerId;
        ReferralCode = referralCode.ToUpperInvariant().Trim();
        Type = type;
        CommissionRate = commissionRate;
        Status = ReferralStatus.Pending;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralCreatedEvent(Id, referrerId, referralCode, type));
    }

    public void RegisterConversion(int referredUserId)
    {
        if (Status != ReferralStatus.Pending)
            throw new InvalidOperationException($"Cannot register conversion for {Status} referral");
        
        if (referredUserId <= 0)
            throw new ArgumentException("Referred user ID must be positive", nameof(referredUserId));
        
        if (referredUserId == ReferrerId)
            throw new InvalidOperationException("User cannot refer themselves");

        ReferredUserId = referredUserId;
        Status = ReferralStatus.Converted;
        ConvertedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralConvertedEvent(Id, ReferrerId, referredUserId, ReferralCode));
    }

    public void CalculateCommission(int paymentId, Money paymentAmount)
    {
        if (Status != ReferralStatus.Converted)
            throw new InvalidOperationException("Can only calculate commission for converted referrals");
        
        if (paymentId <= 0)
            throw new ArgumentException("Payment ID must be positive", nameof(paymentId));
        
        if (paymentAmount == null || paymentAmount.IsZero)
            throw new ArgumentException("Payment amount must be greater than zero", nameof(paymentAmount));

        TriggerPaymentId = paymentId;
        TriggerAmount = paymentAmount;
        CommissionEarned = paymentAmount.Multiply(CommissionRate);
        Status = ReferralStatus.CommissionCalculated;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralCommissionCalculatedEvent(Id, ReferrerId, ReferredUserId, CommissionEarned, paymentId));
    }

    public void PayCommission()
    {
        if (Status != ReferralStatus.CommissionCalculated)
            throw new InvalidOperationException("Can only pay commission that has been calculated");
        
        if (CommissionEarned == null || CommissionEarned.IsZero)
            throw new InvalidOperationException("No commission to pay");

        Status = ReferralStatus.CommissionPaid;
        CommissionPaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralCommissionPaidEvent(Id, ReferrerId, ReferredUserId, CommissionEarned));
    }

    public void MarkAsExpired()
    {
        if (Status != ReferralStatus.Pending)
            throw new InvalidOperationException($"Cannot expire {Status} referral");

        Status = ReferralStatus.Expired;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralExpiredEvent(Id, ReferrerId, ReferralCode));
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Referral is already inactive");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralDeactivatedEvent(Id, ReferrerId, ReferralCode));
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new InvalidOperationException("Referral is already active");
        
        if (Status == ReferralStatus.Expired)
            throw new InvalidOperationException("Cannot reactivate expired referral");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralReactivatedEvent(Id, ReferrerId, ReferralCode));
    }

    public bool IsEligibleForCommission()
    {
        return Status == ReferralStatus.Converted && IsActive;
    }

    public bool HasExpired(TimeSpan expirationPeriod)
    {
        if (Status != ReferralStatus.Pending)
            return false;

        return DateTime.UtcNow - CreatedAt > expirationPeriod;
    }

    public Money GetTotalCommissionEarned()
    {
        if (CommissionEarned == null)
            return Money.Zero();

        return CommissionEarned;
    }

    public TimeSpan? GetConversionTime()
    {
        if (ConvertedAt == null)
            return null;

        return ConvertedAt.Value - CreatedAt;
    }

    public decimal GetConversionRate()
    {
        return Status == ReferralStatus.Converted || 
               Status == ReferralStatus.CommissionCalculated || 
               Status == ReferralStatus.CommissionPaid ? 1.0m : 0.0m;
    }

    public bool CanBeUsedBy(int userId)
    {
        if (!IsActive || Status != ReferralStatus.Pending)
            return false;

        // User cannot use their own referral code
        if (userId == ReferrerId)
            return false;

        return true;
    }

    public void UpdateCommissionRate(decimal newRate)
    {
        if (Status != ReferralStatus.Pending)
            throw new InvalidOperationException("Cannot update commission rate after conversion");
        
        if (newRate < 0 || newRate > 1)
            throw new ArgumentException("Commission rate must be between 0 and 1", nameof(newRate));

        var oldRate = CommissionRate;
        CommissionRate = newRate;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReferralCommissionRateUpdatedEvent(Id, ReferrerId, oldRate, newRate));
    }
}

public enum ReferralStatus
{
    Pending = 1,
    Converted = 2,
    CommissionCalculated = 3,
    CommissionPaid = 4,
    Expired = 5
}

public enum ReferralType
{
    UserRegistration = 1,
    FirstPurchase = 2,
    BookPurchase = 3
}