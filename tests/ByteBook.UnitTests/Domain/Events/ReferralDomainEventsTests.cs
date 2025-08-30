using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Events;

public class ReferralDomainEventsTests
{
    [Fact]
    public void ReferralCreatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referralCode = "REF123";
        var type = ReferralType.UserRegistration;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralCreatedEvent(referralId, referrerId, referralCode, type);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referralCode, domainEvent.ReferralCode);
        Assert.Equal(type, domainEvent.Type);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralConvertedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referredUserId = 2;
        var referralCode = "REF123";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralConvertedEvent(referralId, referrerId, referredUserId, referralCode);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referredUserId, domainEvent.ReferredUserId);
        Assert.Equal(referralCode, domainEvent.ReferralCode);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralCommissionCalculatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referredUserId = 2;
        var commissionAmount = new Money(10.50m, "USD");
        var triggerPaymentId = 100;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralCommissionCalculatedEvent(referralId, referrerId, referredUserId, commissionAmount, triggerPaymentId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referredUserId, domainEvent.ReferredUserId);
        Assert.Equal(commissionAmount, domainEvent.CommissionAmount);
        Assert.Equal(triggerPaymentId, domainEvent.TriggerPaymentId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralCommissionPaidEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referredUserId = 2;
        var commissionAmount = new Money(10.50m, "USD");
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralCommissionPaidEvent(referralId, referrerId, referredUserId, commissionAmount);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referredUserId, domainEvent.ReferredUserId);
        Assert.Equal(commissionAmount, domainEvent.CommissionAmount);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralExpiredEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referralCode = "REF123";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralExpiredEvent(referralId, referrerId, referralCode);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referralCode, domainEvent.ReferralCode);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralDeactivatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referralCode = "REF123";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralDeactivatedEvent(referralId, referrerId, referralCode);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referralCode, domainEvent.ReferralCode);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralReactivatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var referralCode = "REF123";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralReactivatedEvent(referralId, referrerId, referralCode);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(referralCode, domainEvent.ReferralCode);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReferralCommissionRateUpdatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var referralId = 1;
        var referrerId = 1;
        var oldRate = 0.10m;
        var newRate = 0.15m;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReferralCommissionRateUpdatedEvent(referralId, referrerId, oldRate, newRate);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(referralId, domainEvent.ReferralId);
        Assert.Equal(referrerId, domainEvent.ReferrerId);
        Assert.Equal(oldRate, domainEvent.OldRate);
        Assert.Equal(newRate, domainEvent.NewRate);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void AllReferralDomainEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var referralCreatedEvent = new ReferralCreatedEvent(1, 1, "REF123", ReferralType.UserRegistration);
        var referralConvertedEvent = new ReferralConvertedEvent(1, 1, 2, "REF123");
        var referralCommissionCalculatedEvent = new ReferralCommissionCalculatedEvent(1, 1, 2, new Money(10m), 100);
        var referralCommissionPaidEvent = new ReferralCommissionPaidEvent(1, 1, 2, new Money(10m));
        var referralExpiredEvent = new ReferralExpiredEvent(1, 1, "REF123");
        var referralDeactivatedEvent = new ReferralDeactivatedEvent(1, 1, "REF123");
        var referralReactivatedEvent = new ReferralReactivatedEvent(1, 1, "REF123");
        var referralCommissionRateUpdatedEvent = new ReferralCommissionRateUpdatedEvent(1, 1, 0.10m, 0.15m);

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(referralCreatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralConvertedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralCommissionCalculatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralCommissionPaidEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralExpiredEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralDeactivatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralReactivatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(referralCommissionRateUpdatedEvent);
    }
}