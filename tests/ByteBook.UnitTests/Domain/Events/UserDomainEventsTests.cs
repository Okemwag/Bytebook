using ByteBook.Domain.Events;

namespace ByteBook.UnitTests.Domain.Events;

public class UserDomainEventsTests
{
    [Fact]
    public void UserRegisteredEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserRegisteredEvent(userId, email, firstName, lastName);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.Equal(firstName, domainEvent.FirstName);
        Assert.Equal(lastName, domainEvent.LastName);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserEmailVerifiedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserEmailVerifiedEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserPasswordResetEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserPasswordResetEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserProfileUpdatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserProfileUpdatedEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserActivatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserActivatedEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserDeactivatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserDeactivatedEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserLoginEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserLoginEvent(userId, email, ipAddress, userAgent);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.Equal(ipAddress, domainEvent.IpAddress);
        Assert.Equal(userAgent, domainEvent.UserAgent);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void UserLoginEvent_WithoutOptionalParameters_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var email = "test@example.com";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new UserLoginEvent(userId, email);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(email, domainEvent.Email);
        Assert.Null(domainEvent.IpAddress);
        Assert.Null(domainEvent.UserAgent);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void AllUserDomainEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var userRegisteredEvent = new UserRegisteredEvent(1, "test@example.com", "John", "Doe");
        var userEmailVerifiedEvent = new UserEmailVerifiedEvent(1, "test@example.com");
        var userPasswordResetEvent = new UserPasswordResetEvent(1, "test@example.com");
        var userProfileUpdatedEvent = new UserProfileUpdatedEvent(1, "test@example.com");
        var userActivatedEvent = new UserActivatedEvent(1, "test@example.com");
        var userDeactivatedEvent = new UserDeactivatedEvent(1, "test@example.com");
        var userLoginEvent = new UserLoginEvent(1, "test@example.com");

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(userRegisteredEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userEmailVerifiedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userPasswordResetEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userProfileUpdatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userActivatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userDeactivatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(userLoginEvent);
    }
}