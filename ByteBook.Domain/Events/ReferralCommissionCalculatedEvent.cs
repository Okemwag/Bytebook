using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class ReferralCommissionCalculatedEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public int ReferredUserId { get; }
    public Money CommissionAmount { get; }
    public int TriggerPaymentId { get; }
    public DateTime OccurredOn { get; }

    public ReferralCommissionCalculatedEvent(int referralId, int referrerId, int referredUserId, Money commissionAmount, int triggerPaymentId)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferredUserId = referredUserId;
        CommissionAmount = commissionAmount;
        TriggerPaymentId = triggerPaymentId;
        OccurredOn = DateTime.UtcNow;
    }
}