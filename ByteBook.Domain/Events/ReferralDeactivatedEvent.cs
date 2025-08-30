namespace ByteBook.Domain.Events;

public class ReferralDeactivatedEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public string ReferralCode { get; }
    public DateTime OccurredOn { get; }

    public ReferralDeactivatedEvent(int referralId, int referrerId, string referralCode)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferralCode = referralCode;
        OccurredOn = DateTime.UtcNow;
    }
}