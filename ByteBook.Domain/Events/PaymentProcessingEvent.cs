namespace ByteBook.Domain.Events;

public class PaymentProcessingEvent : IDomainEvent
{
    public int PaymentId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public string ExternalTransactionId { get; }
    public DateTime OccurredOn { get; }

    public PaymentProcessingEvent(int paymentId, int userId, int bookId, string externalTransactionId)
    {
        PaymentId = paymentId;
        UserId = userId;
        BookId = bookId;
        ExternalTransactionId = externalTransactionId;
        OccurredOn = DateTime.UtcNow;
    }
}