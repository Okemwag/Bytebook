using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Entities;

public class ReferralTests
{
    [Fact]
    public void Referral_WithValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange
        var referrerId = 1;
        var referralCode = "REF123";
        var type = ReferralType.UserRegistration;
        var commissionRate = 0.15m;

        // Act
        var referral = new Referral(referrerId, referralCode, type, commissionRate);

        // Assert
        Assert.Equal(referrerId, referral.ReferrerId);
        Assert.Equal(referralCode.ToUpperInvariant(), referral.ReferralCode);
        Assert.Equal(type, referral.Type);
        Assert.Equal(commissionRate, referral.CommissionRate);
        Assert.Equal(ReferralStatus.Pending, referral.Status);
        Assert.True(referral.IsActive);
        Assert.Single(referral.DomainEvents);
        Assert.IsType<ReferralCreatedEvent>(referral.DomainEvents.First());
    }

    [Fact]
    public void Referral_WithDefaultCommissionRate_ShouldUseDefault()
    {
        // Arrange
        var referrerId = 1;
        var referralCode = "REF123";
        var type = ReferralType.UserRegistration;

        // Act
        var referral = new Referral(referrerId, referralCode, type);

        // Assert
        Assert.Equal(0.10m, referral.CommissionRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Referral_WithInvalidReferrerId_ShouldThrowArgumentException(int invalidReferrerId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Referral(invalidReferrerId, "REF123", ReferralType.UserRegistration));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Referral_WithEmptyReferralCode_ShouldThrowArgumentException(string invalidCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Referral(1, invalidCode, ReferralType.UserRegistration));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Referral_WithInvalidCommissionRate_ShouldThrowArgumentException(decimal invalidRate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Referral(1, "REF123", ReferralType.UserRegistration, invalidRate));
    }

    [Fact]
    public void RegisterConversion_WithValidUserId_ShouldConvertSuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        var referredUserId = 2;

        // Act
        referral.RegisterConversion(referredUserId);

        // Assert
        Assert.Equal(referredUserId, referral.ReferredUserId);
        Assert.Equal(ReferralStatus.Converted, referral.Status);
        Assert.NotNull(referral.ConvertedAt);
        Assert.Equal(2, referral.DomainEvents.Count);
        Assert.IsType<ReferralConvertedEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void RegisterConversion_WithSameUserAsReferrer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referrerId = 1;
        var referral = new Referral(referrerId, "REF123", ReferralType.UserRegistration);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.RegisterConversion(referrerId));
    }

    [Fact]
    public void RegisterConversion_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.RegisterConversion(2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.RegisterConversion(3));
    }

    [Fact]
    public void CalculateCommission_WithValidPayment_ShouldCalculateCorrectly()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase, 0.20m);
        referral.RegisterConversion(2);
        var paymentId = 100;
        var paymentAmount = new Money(50.00m, "USD");

        // Act
        referral.CalculateCommission(paymentId, paymentAmount);

        // Assert
        Assert.Equal(paymentId, referral.TriggerPaymentId);
        Assert.Equal(paymentAmount, referral.TriggerAmount);
        Assert.Equal(new Money(10.00m, "USD"), referral.CommissionEarned);
        Assert.Equal(ReferralStatus.CommissionCalculated, referral.Status);
        Assert.Equal(3, referral.DomainEvents.Count);
        Assert.IsType<ReferralCommissionCalculatedEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void CalculateCommission_WhenNotConverted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);
        var paymentAmount = new Money(50.00m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.CalculateCommission(100, paymentAmount));
    }

    [Fact]
    public void PayCommission_WhenCommissionCalculated_ShouldPaySuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase, 0.15m);
        referral.RegisterConversion(2);
        referral.CalculateCommission(100, new Money(100.00m, "USD"));

        // Act
        referral.PayCommission();

        // Assert
        Assert.Equal(ReferralStatus.CommissionPaid, referral.Status);
        Assert.NotNull(referral.CommissionPaidAt);
        Assert.Equal(4, referral.DomainEvents.Count);
        Assert.IsType<ReferralCommissionPaidEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void PayCommission_WhenNotCalculated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);
        referral.RegisterConversion(2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.PayCommission());
    }

    [Fact]
    public void MarkAsExpired_WhenPending_ShouldExpireSuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);

        // Act
        referral.MarkAsExpired();

        // Assert
        Assert.Equal(ReferralStatus.Expired, referral.Status);
        Assert.False(referral.IsActive);
        Assert.Equal(2, referral.DomainEvents.Count);
        Assert.IsType<ReferralExpiredEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void MarkAsExpired_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.RegisterConversion(2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.MarkAsExpired());
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateSuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);

        // Act
        referral.Deactivate();

        // Assert
        Assert.False(referral.IsActive);
        Assert.Equal(2, referral.DomainEvents.Count);
        Assert.IsType<ReferralDeactivatedEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void Reactivate_WhenInactive_ShouldReactivateSuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.Deactivate();

        // Act
        referral.Reactivate();

        // Assert
        Assert.True(referral.IsActive);
        Assert.Equal(3, referral.DomainEvents.Count);
        Assert.IsType<ReferralReactivatedEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void Reactivate_WhenExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.MarkAsExpired();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.Reactivate());
    }

    [Fact]
    public void IsEligibleForCommission_WhenConvertedAndActive_ShouldReturnTrue()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);
        referral.RegisterConversion(2);

        // Act & Assert
        Assert.True(referral.IsEligibleForCommission());
    }

    [Fact]
    public void IsEligibleForCommission_WhenNotConverted_ShouldReturnFalse()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);

        // Act & Assert
        Assert.False(referral.IsEligibleForCommission());
    }

    [Fact]
    public void IsEligibleForCommission_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);
        referral.RegisterConversion(2);
        referral.Deactivate();

        // Act & Assert
        Assert.False(referral.IsEligibleForCommission());
    }

    [Fact]
    public void HasExpired_WithinExpirationPeriod_ShouldReturnFalse()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        var expirationPeriod = TimeSpan.FromDays(30);

        // Act
        var hasExpired = referral.HasExpired(expirationPeriod);

        // Assert
        Assert.False(hasExpired);
    }

    [Fact]
    public void HasExpired_WhenConverted_ShouldReturnFalse()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.RegisterConversion(2);
        var expirationPeriod = TimeSpan.FromMilliseconds(1);

        // Act
        var hasExpired = referral.HasExpired(expirationPeriod);

        // Assert
        Assert.False(hasExpired);
    }

    [Fact]
    public void GetTotalCommissionEarned_WithCommission_ShouldReturnAmount()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase, 0.10m);
        referral.RegisterConversion(2);
        referral.CalculateCommission(100, new Money(100.00m, "USD"));

        // Act
        var totalCommission = referral.GetTotalCommissionEarned();

        // Assert
        Assert.Equal(new Money(10.00m, "USD"), totalCommission);
    }

    [Fact]
    public void GetTotalCommissionEarned_WithoutCommission_ShouldReturnZero()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.FirstPurchase);

        // Act
        var totalCommission = referral.GetTotalCommissionEarned();

        // Assert
        Assert.Equal(Money.Zero(), totalCommission);
    }

    [Fact]
    public void GetConversionTime_WhenConverted_ShouldReturnTimeSpan()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.RegisterConversion(2);

        // Act
        var conversionTime = referral.GetConversionTime();

        // Assert
        Assert.NotNull(conversionTime);
        Assert.True(conversionTime.Value.TotalSeconds >= 0);
    }

    [Fact]
    public void GetConversionTime_WhenNotConverted_ShouldReturnNull()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);

        // Act
        var conversionTime = referral.GetConversionTime();

        // Assert
        Assert.Null(conversionTime);
    }

    [Fact]
    public void GetConversionRate_WhenConverted_ShouldReturnOne()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.RegisterConversion(2);

        // Act
        var conversionRate = referral.GetConversionRate();

        // Assert
        Assert.Equal(1.0m, conversionRate);
    }

    [Fact]
    public void GetConversionRate_WhenNotConverted_ShouldReturnZero()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);

        // Act
        var conversionRate = referral.GetConversionRate();

        // Assert
        Assert.Equal(0.0m, conversionRate);
    }

    [Fact]
    public void CanBeUsedBy_ValidUser_ShouldReturnTrue()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        var userId = 2;

        // Act
        var canBeUsed = referral.CanBeUsedBy(userId);

        // Assert
        Assert.True(canBeUsed);
    }

    [Fact]
    public void CanBeUsedBy_SameUserAsReferrer_ShouldReturnFalse()
    {
        // Arrange
        var referrerId = 1;
        var referral = new Referral(referrerId, "REF123", ReferralType.UserRegistration);

        // Act
        var canBeUsed = referral.CanBeUsedBy(referrerId);

        // Assert
        Assert.False(canBeUsed);
    }

    [Fact]
    public void CanBeUsedBy_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);
        referral.Deactivate();

        // Act
        var canBeUsed = referral.CanBeUsedBy(2);

        // Assert
        Assert.False(canBeUsed);
    }

    [Fact]
    public void UpdateCommissionRate_WhenPending_ShouldUpdateSuccessfully()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration, 0.10m);
        var newRate = 0.15m;

        // Act
        referral.UpdateCommissionRate(newRate);

        // Assert
        Assert.Equal(newRate, referral.CommissionRate);
        Assert.Equal(2, referral.DomainEvents.Count);
        Assert.IsType<ReferralCommissionRateUpdatedEvent>(referral.DomainEvents.Last());
    }

    [Fact]
    public void UpdateCommissionRate_WhenConverted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration, 0.10m);
        referral.RegisterConversion(2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => referral.UpdateCommissionRate(0.15m));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void UpdateCommissionRate_WithInvalidRate_ShouldThrowArgumentException(decimal invalidRate)
    {
        // Arrange
        var referral = new Referral(1, "REF123", ReferralType.UserRegistration);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => referral.UpdateCommissionRate(invalidRate));
    }
}