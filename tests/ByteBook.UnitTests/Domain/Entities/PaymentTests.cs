using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Entities;

public class PaymentTests
{
    [Fact]
    public void Payment_WithValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;
        var amount = new Money(10.50m, "USD");
        var paymentType = PaymentType.PerPage;
        var provider = PaymentProvider.Stripe;

        // Act
        var payment = new Payment(userId, bookId, amount, paymentType, provider);

        // Assert
        Assert.Equal(userId, payment.UserId);
        Assert.Equal(bookId, payment.BookId);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(paymentType, payment.PaymentType);
        Assert.Equal(provider, payment.Provider);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Single(payment.DomainEvents);
        Assert.IsType<PaymentInitiatedEvent>(payment.DomainEvents.First());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Payment_WithInvalidUserId_ShouldThrowArgumentException(int invalidUserId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Payment(invalidUserId, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Payment_WithInvalidBookId_ShouldThrowArgumentException(int invalidBookId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Payment(1, invalidBookId, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe));
    }

    [Fact]
    public void Payment_WithZeroAmount_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Payment(1, 1, Money.Zero(), PaymentType.PerPage, PaymentProvider.Stripe));
    }

    [Fact]
    public void MarkAsProcessing_WithValidTransaction_ShouldUpdateStatus()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        var transactionId = "txn_123456";

        // Act
        payment.MarkAsProcessing(transactionId);

        // Assert
        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal(transactionId, payment.ExternalTransactionId);
        Assert.Equal(2, payment.DomainEvents.Count);
        Assert.IsType<PaymentProcessingEvent>(payment.DomainEvents.Last());
    }

    [Fact]
    public void MarkAsProcessing_WithEmptyTransactionId_ShouldThrowArgumentException()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => payment.MarkAsProcessing(""));
    }

    [Fact]
    public void MarkAsProcessing_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => payment.MarkAsProcessing("txn_456"));
    }

    [Fact]
    public void MarkAsCompleted_WhenProcessing_ShouldUpdateStatus()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");

        // Act
        payment.MarkAsCompleted();

        // Assert
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.NotNull(payment.ProcessedAt);
        Assert.Equal(3, payment.DomainEvents.Count);
        Assert.IsType<PaymentCompletedEvent>(payment.DomainEvents.Last());
    }

    [Fact]
    public void MarkAsCompleted_WhenNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => payment.MarkAsCompleted());
    }

    [Fact]
    public void MarkAsFailed_WithValidReason_ShouldUpdateStatus()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        var failureReason = "Insufficient funds";

        // Act
        payment.MarkAsFailed(failureReason);

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(failureReason, payment.FailureReason);
        Assert.Equal(2, payment.DomainEvents.Count);
        Assert.IsType<PaymentFailedEvent>(payment.DomainEvents.Last());
    }

    [Fact]
    public void ProcessRefund_WithValidAmount_ShouldUpdateRefund()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        var refundAmount = new Money(5m);

        // Act
        payment.ProcessRefund(refundAmount);

        // Assert
        Assert.Equal(refundAmount, payment.RefundedAmount);
        Assert.NotNull(payment.RefundedAt);
        Assert.Equal(PaymentStatus.Completed, payment.Status); // Partial refund
        Assert.Equal(4, payment.DomainEvents.Count);
        Assert.IsType<PaymentRefundedEvent>(payment.DomainEvents.Last());
    }

    [Fact]
    public void ProcessRefund_WithFullAmount_ShouldMarkAsRefunded()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        var refundAmount = new Money(10m);

        // Act
        payment.ProcessRefund(refundAmount);

        // Assert
        Assert.Equal(refundAmount, payment.RefundedAmount);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void ProcessRefund_WhenNotCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        var refundAmount = new Money(5m);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => payment.ProcessRefund(refundAmount));
    }

    [Fact]
    public void ProcessRefund_WithExcessiveAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        var refundAmount = new Money(15m);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => payment.ProcessRefund(refundAmount));
    }

    [Fact]
    public void CalculateAuthorEarnings_WithDefaultCommission_ShouldCalculateCorrectly()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();

        // Act
        var earnings = payment.CalculateAuthorEarnings();

        // Assert
        Assert.Equal(new Money(85m), earnings); // 100 - 15% commission
    }

    [Fact]
    public void CalculateAuthorEarnings_WithCustomCommission_ShouldCalculateCorrectly()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();

        // Act
        var earnings = payment.CalculateAuthorEarnings(0.20m); // 20% commission

        // Assert
        Assert.Equal(new Money(80m), earnings);
    }

    [Fact]
    public void CalculateAuthorEarnings_WithRefund_ShouldCalculateCorrectly()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        payment.ProcessRefund(new Money(20m));

        // Act
        var earnings = payment.CalculateAuthorEarnings();

        // Assert
        Assert.Equal(new Money(68m), earnings); // (100 - 20) * 0.85
    }

    [Fact]
    public void CalculateAuthorEarnings_WhenNotCompleted_ShouldReturnZero()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);

        // Act
        var earnings = payment.CalculateAuthorEarnings();

        // Assert
        Assert.Equal(Money.Zero(), earnings);
    }

    [Fact]
    public void IsRefundable_WhenCompleted_ShouldReturnTrue()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();

        // Act & Assert
        Assert.True(payment.IsRefundable());
    }

    [Fact]
    public void IsRefundable_WhenFullyRefunded_ShouldReturnFalse()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        payment.ProcessRefund(new Money(100m));

        // Act & Assert
        Assert.False(payment.IsRefundable());
    }

    [Fact]
    public void GetRefundableAmount_ShouldReturnCorrectAmount()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        payment.MarkAsProcessing("txn_123");
        payment.MarkAsCompleted();
        payment.ProcessRefund(new Money(30m));

        // Act
        var refundableAmount = payment.GetRefundableAmount();

        // Assert
        Assert.Equal(new Money(70m), refundableAmount);
    }

    [Fact]
    public void AssociateWithReadingSession_WithValidId_ShouldSetProperty()
    {
        // Arrange
        var payment = new Payment(1, 1, new Money(100m), PaymentType.PerPage, PaymentProvider.Stripe);
        var readingSessionId = 123;

        // Act
        payment.AssociateWithReadingSession(readingSessionId);

        // Assert
        Assert.Equal(readingSessionId, payment.ReadingSessionId);
    }
}