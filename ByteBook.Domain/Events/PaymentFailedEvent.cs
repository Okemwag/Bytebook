namespace ByteBook.Domain.Events;

public class PaymentFailedEvent : IDomainEvent
{
    public int PaymentId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public string FailureReason { get; }
    public DateTime OccurredOn { get; }

    public PaymentFailedEvent(int paymentId, int userId, int bookId, string failureReason)
    {
        PaymentId = paymentId;
        UserId = userId;
        BookId = bookId;
        FailureReason = failureReason;
        OccurredOn = DateTime.UtcNow;
    }
}