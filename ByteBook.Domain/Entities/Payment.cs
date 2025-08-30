using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Entities;

public class Payment : BaseEntity
{
    public int UserId { get; private set; }
    public int BookId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string? ExternalTransactionId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public Money? RefundedAmount { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public int? ReadingSessionId { get; private set; }

    // Navigation properties - will be uncommented when entities are created
    // public User User { get; private set; }
    // public Book Book { get; private set; }
    // public Reading? ReadingSession { get; private set; }

    private Payment()
    {
        Amount = Money.Zero();
    } // EF Core

    public Payment(int userId, int bookId, Money amount, PaymentType paymentType, PaymentProvider provider)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be positive", nameof(userId));
        
        if (bookId <= 0)
            throw new ArgumentException("Book ID must be positive", nameof(bookId));
        
        if (amount == null || amount.IsZero)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        UserId = userId;
        BookId = bookId;
        Amount = amount;
        PaymentType = paymentType;
        Provider = provider;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentInitiatedEvent(Id, userId, bookId, amount, paymentType, provider));
    }

    public void MarkAsProcessing(string externalTransactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot mark payment as processing. Current status: {Status}");
        
        if (string.IsNullOrWhiteSpace(externalTransactionId))
            throw new ArgumentException("External transaction ID cannot be empty", nameof(externalTransactionId));

        Status = PaymentStatus.Processing;
        ExternalTransactionId = externalTransactionId;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentProcessingEvent(Id, UserId, BookId, externalTransactionId));
    }

    public void MarkAsCompleted()
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot mark payment as completed. Current status: {Status}");

        Status = PaymentStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentCompletedEvent(Id, UserId, BookId, Amount, ExternalTransactionId));
    }

    public void MarkAsFailed(string failureReason)
    {
        if (Status == PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot mark completed payment as failed");
        
        if (Status == PaymentStatus.Refunded)
            throw new InvalidOperationException("Cannot mark refunded payment as failed");

        if (string.IsNullOrWhiteSpace(failureReason))
            throw new ArgumentException("Failure reason cannot be empty", nameof(failureReason));

        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(Id, UserId, BookId, failureReason));
    }

    public void ProcessRefund(Money refundAmount)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Can only refund completed payments");
        
        if (refundAmount == null || refundAmount.IsZero)
            throw new ArgumentException("Refund amount must be greater than zero", nameof(refundAmount));
        
        if (refundAmount.Currency != Amount.Currency)
            throw new ArgumentException("Refund currency must match payment currency", nameof(refundAmount));
        
        if (refundAmount.Amount > Amount.Amount)
            throw new ArgumentException("Refund amount cannot exceed payment amount", nameof(refundAmount));

        var totalRefunded = (RefundedAmount?.Amount ?? 0) + refundAmount.Amount;
        if (totalRefunded > Amount.Amount)
            throw new ArgumentException("Total refund amount cannot exceed payment amount", nameof(refundAmount));

        RefundedAmount = RefundedAmount == null ? refundAmount : RefundedAmount.Add(refundAmount);
        RefundedAt = DateTime.UtcNow;
        
        if (RefundedAmount.Amount >= Amount.Amount)
        {
            Status = PaymentStatus.Refunded;
        }
        
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentRefundedEvent(Id, UserId, BookId, refundAmount, RefundedAmount));
    }

    public Money CalculateAuthorEarnings(decimal platformCommissionRate = 0.15m)
    {
        if (Status != PaymentStatus.Completed)
            return Money.Zero(Amount.Currency);
        
        if (platformCommissionRate < 0 || platformCommissionRate > 1)
            throw new ArgumentException("Commission rate must be between 0 and 1", nameof(platformCommissionRate));

        var netAmount = RefundedAmount == null ? Amount : Amount.Subtract(RefundedAmount);
        var platformCommission = netAmount.Multiply(platformCommissionRate);
        return netAmount.Subtract(platformCommission);
    }

    public void AssociateWithReadingSession(int readingSessionId)
    {
        if (readingSessionId <= 0)
            throw new ArgumentException("Reading session ID must be positive", nameof(readingSessionId));

        ReadingSessionId = readingSessionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsRefundable()
    {
        return Status == PaymentStatus.Completed && 
               (RefundedAmount == null || RefundedAmount.Amount < Amount.Amount);
    }

    public Money GetRefundableAmount()
    {
        if (!IsRefundable())
            return Money.Zero(Amount.Currency);

        return RefundedAmount == null ? Amount : Amount.Subtract(RefundedAmount);
    }
}

public enum PaymentType
{
    PerPage = 1,
    PerHour = 2
}

public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Refunded = 5
}

public enum PaymentProvider
{
    Stripe = 1,
    PayPal = 2,
    MPesa = 3
}