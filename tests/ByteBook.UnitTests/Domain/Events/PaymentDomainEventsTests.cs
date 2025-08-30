using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Events;

public class PaymentDomainEventsTests
{
    [Fact]
    public void PaymentInitiatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var paymentId = 1;
        var userId = 1;
        var bookId = 1;
        var amount = new Money(10.50m, "USD");
        var paymentType = PaymentType.PerPage;
        var provider = PaymentProvider.Stripe;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new PaymentInitiatedEvent(paymentId, userId, bookId, amount, paymentType, provider);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(paymentId, domainEvent.PaymentId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(paymentType, domainEvent.PaymentType);
        Assert.Equal(provider, domainEvent.Provider);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void PaymentProcessingEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var paymentId = 1;
        var userId = 1;
        var bookId = 1;
        var externalTransactionId = "txn_123456";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new PaymentProcessingEvent(paymentId, userId, bookId, externalTransactionId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(paymentId, domainEvent.PaymentId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(externalTransactionId, domainEvent.ExternalTransactionId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void PaymentCompletedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var paymentId = 1;
        var userId = 1;
        var bookId = 1;
        var amount = new Money(10.50m, "USD");
        var externalTransactionId = "txn_123456";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new PaymentCompletedEvent(paymentId, userId, bookId, amount, externalTransactionId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(paymentId, domainEvent.PaymentId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(externalTransactionId, domainEvent.ExternalTransactionId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void PaymentFailedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var paymentId = 1;
        var userId = 1;
        var bookId = 1;
        var failureReason = "Insufficient funds";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new PaymentFailedEvent(paymentId, userId, bookId, failureReason);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(paymentId, domainEvent.PaymentId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(failureReason, domainEvent.FailureReason);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void PaymentRefundedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var paymentId = 1;
        var userId = 1;
        var bookId = 1;
        var refundAmount = new Money(5.00m, "USD");
        var totalRefunded = new Money(5.00m, "USD");
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new PaymentRefundedEvent(paymentId, userId, bookId, refundAmount, totalRefunded);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(paymentId, domainEvent.PaymentId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(refundAmount, domainEvent.RefundAmount);
        Assert.Equal(totalRefunded, domainEvent.TotalRefunded);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void AllPaymentDomainEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var paymentInitiatedEvent = new PaymentInitiatedEvent(1, 1, 1, new Money(10m), PaymentType.PerPage, PaymentProvider.Stripe);
        var paymentProcessingEvent = new PaymentProcessingEvent(1, 1, 1, "txn_123");
        var paymentCompletedEvent = new PaymentCompletedEvent(1, 1, 1, new Money(10m), "txn_123");
        var paymentFailedEvent = new PaymentFailedEvent(1, 1, 1, "Failed");
        var paymentRefundedEvent = new PaymentRefundedEvent(1, 1, 1, new Money(5m), new Money(5m));

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(paymentInitiatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(paymentProcessingEvent);
        Assert.IsAssignableFrom<IDomainEvent>(paymentCompletedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(paymentFailedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(paymentRefundedEvent);
    }
}