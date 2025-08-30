namespace ByteBook.Domain.Events;

public class ReferralCommissionRateUpdatedEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public decimal OldRate { get; }
    public decimal NewRate { get; }
    public DateTime OccurredOn { get; }

    public ReferralCommissionRateUpdatedEvent(int referralId, int referrerId, decimal oldRate, decimal newRate)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        OldRate = oldRate;
        NewRate = newRate;
        OccurredOn = DateTime.UtcNow;
    }
}