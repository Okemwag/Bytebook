using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class PaymentRefundedEvent : IDomainEvent
{
    public int PaymentId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public Money RefundAmount { get; }
    public Money TotalRefunded { get; }
    public DateTime OccurredOn { get; }

    public PaymentRefundedEvent(int paymentId, int userId, int bookId, Money refundAmount, Money totalRefunded)
    {
        PaymentId = paymentId;
        UserId = userId;
        BookId = bookId;
        RefundAmount = refundAmount;
        TotalRefunded = totalRefunded;
        OccurredOn = DateTime.UtcNow;
    }
}