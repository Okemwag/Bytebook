using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class ReferralCommissionPaidEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public int ReferredUserId { get; }
    public Money CommissionAmount { get; }
    public DateTime OccurredOn { get; }

    public ReferralCommissionPaidEvent(int referralId, int referrerId, int referredUserId, Money commissionAmount)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferredUserId = referredUserId;
        CommissionAmount = commissionAmount;
        OccurredOn = DateTime.UtcNow;
    }
}