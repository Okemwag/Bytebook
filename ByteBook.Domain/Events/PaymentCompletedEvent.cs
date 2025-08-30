using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class PaymentCompletedEvent : IDomainEvent
{
    public int PaymentId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public Money Amount { get; }
    public string? ExternalTransactionId { get; }
    public DateTime OccurredOn { get; }

    public PaymentCompletedEvent(int paymentId, int userId, int bookId, Money amount, string? externalTransactionId)
    {
        PaymentId = paymentId;
        UserId = userId;
        BookId = bookId;
        Amount = amount;
        ExternalTransactionId = externalTransactionId;
        OccurredOn = DateTime.UtcNow;
    }
}