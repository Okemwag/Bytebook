using ByteBook.Domain.Entities;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class PaymentInitiatedEvent : IDomainEvent
{
    public int PaymentId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public Money Amount { get; }
    public PaymentType PaymentType { get; }
    public PaymentProvider Provider { get; }
    public DateTime OccurredOn { get; }

    public PaymentInitiatedEvent(int paymentId, int userId, int bookId, Money amount, PaymentType paymentType, PaymentProvider provider)
    {
        PaymentId = paymentId;
        UserId = userId;
        BookId = bookId;
        Amount = amount;
        PaymentType = paymentType;
        Provider = provider;
        OccurredOn = DateTime.UtcNow;
    }
}