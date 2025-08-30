namespace ByteBook.Domain.Events;

public class ReferralExpiredEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public string ReferralCode { get; }
    public DateTime OccurredOn { get; }

    public ReferralExpiredEvent(int referralId, int referrerId, string referralCode)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferralCode = referralCode;
        OccurredOn = DateTime.UtcNow;
    }
}