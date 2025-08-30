using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Events;

public class BookDomainEventsTests
{
    [Fact]
    public void BookCreatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Test Book";
        var authorId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookCreatedEvent(bookId, title, authorId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookUpdatedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Updated Book";
        var authorId = 1;
        var titleChanged = true;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookUpdatedEvent(bookId, title, authorId, titleChanged);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.Equal(titleChanged, domainEvent.TitleChanged);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookPublishedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Published Book";
        var authorId = 1;
        var publishedAt = DateTime.UtcNow;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookPublishedEvent(bookId, title, authorId, publishedAt);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.Equal(publishedAt, domainEvent.PublishedAt);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookPricingChangedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Priced Book";
        var authorId = 1;
        var oldPricePerPage = new Money(0.25m, "USD");
        var oldPricePerHour = new Money(5.00m, "USD");
        var newPricePerPage = new Money(0.50m, "USD");
        var newPricePerHour = new Money(10.00m, "USD");
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookPricingChangedEvent(bookId, title, authorId, 
            oldPricePerPage, oldPricePerHour, newPricePerPage, newPricePerHour);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.Equal(oldPricePerPage, domainEvent.OldPricePerPage);
        Assert.Equal(oldPricePerHour, domainEvent.OldPricePerHour);
        Assert.Equal(newPricePerPage, domainEvent.NewPricePerPage);
        Assert.Equal(newPricePerHour, domainEvent.NewPricePerHour);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookUnpublishedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Unpublished Book";
        var authorId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookUnpublishedEvent(bookId, title, authorId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookArchivedEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Archived Book";
        var authorId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookArchivedEvent(bookId, title, authorId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void BookRestoredEvent_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bookId = 1;
        var title = "Restored Book";
        var authorId = 1;
        var beforeCreation = DateTime.UtcNow;

        // Act
        var domainEvent = new BookRestoredEvent(bookId, title, authorId);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(bookId, domainEvent.BookId);
        Assert.Equal(title, domainEvent.Title);
        Assert.Equal(authorId, domainEvent.AuthorId);
        Assert.True(domainEvent.OccurredOn >= beforeCreation && domainEvent.OccurredOn <= afterCreation);
    }

    [Fact]
    public void AllBookDomainEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var bookCreatedEvent = new BookCreatedEvent(1, "Title", 1);
        var bookUpdatedEvent = new BookUpdatedEvent(1, "Title", 1, false);
        var bookPublishedEvent = new BookPublishedEvent(1, "Title", 1, DateTime.UtcNow);
        var bookPricingChangedEvent = new BookPricingChangedEvent(1, "Title", 1, null, null, null, null);
        var bookUnpublishedEvent = new BookUnpublishedEvent(1, "Title", 1);
        var bookArchivedEvent = new BookArchivedEvent(1, "Title", 1);
        var bookRestoredEvent = new BookRestoredEvent(1, "Title", 1);

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(bookCreatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookUpdatedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookPublishedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookPricingChangedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookUnpublishedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookArchivedEvent);
        Assert.IsAssignableFrom<IDomainEvent>(bookRestoredEvent);
    }
}