namespace ByteBook.Domain.Events;

public class ReferralConvertedEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public int ReferredUserId { get; }
    public string ReferralCode { get; }
    public DateTime OccurredOn { get; }

    public ReferralConvertedEvent(int referralId, int referrerId, int referredUserId, string referralCode)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferredUserId = referredUserId;
        ReferralCode = referralCode;
        OccurredOn = DateTime.UtcNow;
    }
}