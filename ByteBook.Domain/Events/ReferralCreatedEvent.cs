using ByteBook.Domain.Entities;

namespace ByteBook.Domain.Events;

public class ReferralCreatedEvent : IDomainEvent
{
    public int ReferralId { get; }
    public int ReferrerId { get; }
    public string ReferralCode { get; }
    public ReferralType Type { get; }
    public DateTime OccurredOn { get; }

    public ReferralCreatedEvent(int referralId, int referrerId, string referralCode, ReferralType type)
    {
        ReferralId = referralId;
        ReferrerId = referrerId;
        ReferralCode = referralCode;
        Type = type;
        OccurredOn = DateTime.UtcNow;
    }
}