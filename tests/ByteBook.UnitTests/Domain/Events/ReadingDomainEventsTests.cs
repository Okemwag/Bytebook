using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Events;

public class ReadingDomainEventsTests
{
    [Fact]
    public void ReadingSessionStartedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var startTime = DateTime.UtcNow;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingSessionStartedEvent(readingId, userId, bookId, startTime);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(startTime, domainEvent.StartTime);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingProgressUpdatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var currentPage = 10;
        var totalPagesRead = 10;
        var previousPagesRead = 5;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingProgressUpdatedEvent(readingId, userId, bookId, currentPage, totalPagesRead, previousPagesRead);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(currentPage, domainEvent.CurrentPage);
        Assert.Equal(totalPagesRead, domainEvent.TotalPagesRead);
        Assert.Equal(previousPagesRead, domainEvent.PreviousPagesRead);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingSessionEndedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow;
        var pagesRead = 15;
        var timeSpentMinutes = 30;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingSessionEndedEvent(readingId, userId, bookId, startTime, endTime, pagesRead, timeSpentMinutes);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(startTime, domainEvent.StartTime);
        Assert.Equal(endTime, domainEvent.EndTime);
        Assert.Equal(pagesRead, domainEvent.PagesRead);
        Assert.Equal(timeSpentMinutes, domainEvent.TimeSpentMinutes);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingSessionPausedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingSessionPausedEvent(readingId, userId, bookId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingSessionResumedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingSessionResumedEvent(readingId, userId, bookId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingCompletedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var pagesRead = 25;
        var timeSpentMinutes = 45;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingCompletedEvent(readingId, userId, bookId, pagesRead, timeSpentMinutes);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(pagesRead, domainEvent.PagesRead);
        Assert.Equal(timeSpentMinutes, domainEvent.TimeSpentMinutes);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingChargedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var amount = new Money(5.50m, "USD");
        var chargeType = PaymentType.PerPage;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingChargedEvent(readingId, userId, bookId, amount, chargeType);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(amount, domainEvent.Amount);
        Assert.Equal(chargeType, domainEvent.ChargeType);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void ReadingSessionTerminatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var readingId = 1;
        var userId = 1;
        var bookId = 1;
        var reason = "Session timeout";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new ReadingSessionTerminatedEvent(readingId, userId, bookId, reason);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(readingId, domainEvent.ReadingId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(reason, domainEvent.Reason);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void AllReadingDomainEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var readingSessionStartedEvent = new ReadingSessionStartedEvent(1, 1, 1, DateTime.UtcNow);
        var readingProgressUpdatedEvent = new ReadingProgressUpdatedEvent(1, 1, 1, 10, 10, 5);
        var readingSessionEndedEvent = new ReadingSessionEndedEvent(1, 1, 1, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow, 10, 30);
        var readingSessionPausedEvent = new ReadingSessionPausedEvent(1, 1, 1);
        var readingSessionResumedEvent = new ReadingSessionResumedEvent(1, 1, 1);
        var readingCompletedEvent = new ReadingCompletedEvent(1, 1, 1, 25, 45);
        var readingChargedEvent = new ReadingChargedEvent(1, 1, 1, new Money(5m), PaymentType.PerPage);
        var readingSessionTerminatedEvent = new ReadingSessionTerminatedEvent(1, 1, 1, "Timeout");

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(readingSessionStartedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingProgressUpdatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingSessionEndedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingSessionPausedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingSessionResumedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingCompletedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingChargedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(readingSessionTerminatedEvent);
    }
}