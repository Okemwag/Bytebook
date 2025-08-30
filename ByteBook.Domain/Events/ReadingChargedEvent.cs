using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class ReadingChargedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public Money Amount { get; }
    public PaymentType ChargeType { get; }
    public DateTime OccurredOn { get; }

    public ReadingChargedEvent(int readingId, int userId, int bookId, Money amount, PaymentType chargeType)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        Amount = amount;
        ChargeType = chargeType;
        OccurredOn = DateTime.UtcNow;
    }
}