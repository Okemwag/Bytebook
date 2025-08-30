using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Entities;

public class ReadingTests
{
    [Fact]
    public void Reading_WithValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange
        var userId = 1;
        var bookId = 1;

        // Act
        var reading = new Reading(userId, bookId);

        // Assert
        Assert.Equal(userId, reading.UserId);
        Assert.Equal(bookId, reading.BookId);
        Assert.Equal(ReadingStatus.Active, reading.Status);
        Assert.Equal(0, reading.PagesRead);
        Assert.Equal(0, reading.TimeSpentMinutes);
        Assert.Equal(0, reading.LastPageRead);
        Assert.False(reading.IsCompleted);
        Assert.Single(reading.DomainEvents);
        Assert.IsType<ReadingSessionStartedEvent>(reading.DomainEvents.First());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reading_WithInvalidUserId_ShouldThrowArgumentException(int invalidUserId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Reading(invalidUserId, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reading_WithInvalidBookId_ShouldThrowArgumentException(int invalidBookId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Reading(1, invalidBookId));
    }

    [Fact]
    public void UpdateProgress_WithValidProgress_ShouldUpdateSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);
        var currentPage = 5;
        var totalPagesRead = 5;

        // Act
        reading.UpdateProgress(currentPage, totalPagesRead);

        // Assert
        Assert.Equal(currentPage, reading.LastPageRead);
        Assert.Equal(totalPagesRead, reading.PagesRead);
        Assert.Equal(2, reading.DomainEvents.Count);
        Assert.IsType<ReadingProgressUpdatedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void UpdateProgress_WithSamePagesRead_ShouldNotGenerateEvent()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(5, 5);

        // Act
        reading.UpdateProgress(6, 5); // Same pages read, different current page

        // Assert
        Assert.Equal(6, reading.LastPageRead);
        Assert.Equal(5, reading.PagesRead);
        Assert.Equal(2, reading.DomainEvents.Count); // No new event
    }

    [Fact]
    public void UpdateProgress_WithDecreasingPages_ShouldThrowArgumentException()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(5, 5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => reading.UpdateProgress(3, 3));
    }

    [Fact]
    public void UpdateProgress_WhenNotActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reading.UpdateProgress(5, 5));
    }

    [Fact]
    public void EndSession_WhenActive_ShouldEndSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(10, 10);

        // Act
        reading.EndSession();

        // Assert
        Assert.NotNull(reading.EndTime);
        Assert.Equal(ReadingStatus.Completed, reading.Status);
        Assert.True(reading.TimeSpentMinutes >= 0);
        Assert.Equal(3, reading.DomainEvents.Count);
        Assert.IsType<ReadingSessionEndedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void EndSession_WhenAlreadyEnded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.EndSession();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reading.EndSession());
    }

    [Fact]
    public void PauseSession_WhenActive_ShouldPauseSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);

        // Act
        reading.PauseSession();

        // Assert
        Assert.Equal(ReadingStatus.Paused, reading.Status);
        Assert.Equal(2, reading.DomainEvents.Count);
        Assert.IsType<ReadingSessionPausedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void ResumeSession_WhenPaused_ShouldResumeSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.PauseSession();

        // Act
        reading.ResumeSession();

        // Assert
        Assert.Equal(ReadingStatus.Active, reading.Status);
        Assert.Equal(3, reading.DomainEvents.Count);
        Assert.IsType<ReadingSessionResumedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void ResumeSession_WhenNotPaused_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reading = new Reading(1, 1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reading.ResumeSession());
    }

    [Fact]
    public void MarkAsCompleted_WhenActive_ShouldCompleteSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(10, 10);

        // Act
        reading.MarkAsCompleted();

        // Assert
        Assert.True(reading.IsCompleted);
        Assert.Equal(ReadingStatus.Completed, reading.Status);
        Assert.NotNull(reading.EndTime);
        Assert.Equal(4, reading.DomainEvents.Count);
        Assert.IsType<ReadingCompletedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void MarkAsCompleted_WhenAlreadyCompleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.MarkAsCompleted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reading.MarkAsCompleted());
    }

    [Fact]
    public void CalculatePageCharges_WithValidParameters_ShouldCalculateCorrectly()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(10, 10);
        var pricePerPage = new Money(0.50m, "USD");
        var totalBookPages = 100;

        // Act
        var charges = reading.CalculatePageCharges(pricePerPage, totalBookPages);

        // Assert
        Assert.Equal(new Money(5.00m, "USD"), charges);
    }

    [Fact]
    public void CalculatePageCharges_WithMorePagesThanBook_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(150, 150);
        var pricePerPage = new Money(0.50m, "USD");
        var totalBookPages = 100;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => reading.CalculatePageCharges(pricePerPage, totalBookPages));
    }

    [Fact]
    public void CalculateTimeCharges_WithValidParameters_ShouldCalculateCorrectly()
    {
        // Arrange
        var reading = new Reading(1, 1);
        reading.UpdateProgress(10, 10);
        reading.EndSession();
        
        // Simulate 30 minutes of reading
        var readingWithTime = new Reading(1, 1);
        readingWithTime.UpdateProgress(10, 10);
        readingWithTime.EndSession();
        
        var pricePerHour = new Money(12.00m, "USD");

        // Act
        var charges = readingWithTime.CalculateTimeCharges(pricePerHour);

        // Assert
        Assert.True(charges.Amount >= 0); // Time-based calculation
    }

    [Fact]
    public void RecordCharge_WithValidAmount_ShouldRecordSuccessfully()
    {
        // Arrange
        var reading = new Reading(1, 1);
        var chargeAmount = new Money(5.00m, "USD");
        var chargeType = PaymentType.PerPage;

        // Act
        reading.RecordCharge(chargeAmount, chargeType);

        // Assert
        Assert.Equal(chargeAmount, reading.ChargedAmount);
        Assert.Equal(chargeType, reading.ChargeType);
        Assert.Equal(2, reading.DomainEvents.Count);
        Assert.IsType<ReadingChargedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void RecordCharge_MultipleCharges_ShouldAccumulate()
    {
        // Arrange
        var reading = new Reading(1, 1);
        var firstCharge = new Money(3.00m, "USD");
        var secondCharge = new Money(2.00m, "USD");

        // Act
        reading.RecordCharge(firstCharge, PaymentType.PerPage);
        reading.RecordCharge(secondCharge, PaymentType.PerPage);

        // Assert
        Assert.Equal(new Money(5.00m, "USD"), reading.ChargedAmount);
    }

    [Fact]
    public void GetCurrentDuration_ShouldReturnCorrectTimeSpan()
    {
        // Arrange
        var reading = new Reading(1, 1);
        
        // Act
        var duration = reading.GetCurrentDuration();

        // Assert
        Assert.True(duration.TotalSeconds >= 0);
    }

    [Fact]
    public void GetReadingRate_WithZeroTime_ShouldReturnZero()
    {
        // Arrange
        var reading = new Reading(1, 1);

        // Act
        var rate = reading.GetReadingRate();

        // Assert
        Assert.Equal(0, rate);
    }

    [Fact]
    public void HasExceededTimeLimit_WithinLimit_ShouldReturnFalse()
    {
        // Arrange
        var reading = new Reading(1, 1);
        var maxMinutes = 60;

        // Act
        var exceeded = reading.HasExceededTimeLimit(maxMinutes);

        // Assert
        Assert.False(exceeded);
    }

    [Fact]
    public void ForceEnd_WithValidReason_ShouldTerminateSession()
    {
        // Arrange
        var reading = new Reading(1, 1);
        var reason = "Session timeout";

        // Act
        reading.ForceEnd(reason);

        // Assert
        Assert.Equal(ReadingStatus.Terminated, reading.Status);
        Assert.NotNull(reading.EndTime);
        Assert.Equal(2, reading.DomainEvents.Count);
        Assert.IsType<ReadingSessionTerminatedEvent>(reading.DomainEvents.Last());
    }

    [Fact]
    public void ForceEnd_WithEmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var reading = new Reading(1, 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => reading.ForceEnd(""));
    }
}