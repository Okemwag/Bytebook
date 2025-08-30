using ByteBook.Domain.Entities;
using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.Entities;

public class BookTests
{
    [Fact]
    public void Book_WithValidParameters_ShouldCreateSuccessfully()
    {
        // Arrange
        var title = "Test Book";
        var description = "Test Description";
        var authorId = 1;
        var category = "Technology";

        // Act
        var book = new Book(title, description, authorId, category);

        // Assert
        Assert.Equal(title, book.Title);
        Assert.Equal(description, book.Description);
        Assert.Equal(authorId, book.AuthorId);
        Assert.Equal(category, book.Category);
        Assert.Equal(BookStatus.Draft, book.Status);
        Assert.False(book.IsPublished);
        Assert.True(book.IsActive);
        Assert.Equal(0, book.TotalPages);
        Assert.Null(book.PricePerPage);
        Assert.Null(book.PricePerHour);
        Assert.Single(book.DomainEvents);
        Assert.IsType<BookCreatedEvent>(book.DomainEvents.First());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Book_WithEmptyTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Book(invalidTitle, "Description", 1, "Category"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Book_WithEmptyDescription_ShouldThrowArgumentException(string invalidDescription)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Book("Title", invalidDescription, 1, "Category"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Book_WithInvalidAuthorId_ShouldThrowArgumentException(int invalidAuthorId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Book("Title", "Description", invalidAuthorId, "Category"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Book_WithEmptyCategory_ShouldThrowArgumentException(string invalidCategory)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Book("Title", "Description", 1, invalidCategory));
    }

    [Fact]
    public void UpdateContent_WithValidParameters_ShouldUpdateSuccessfully()
    {
        // Arrange
        var book = new Book("Old Title", "Old Description", 1, "Category");
        var newTitle = "New Title";
        var newDescription = "New Description";
        var contentUrl = "https://example.com/content.pdf";
        var totalPages = 100;

        // Act
        book.UpdateContent(newTitle, newDescription, contentUrl, totalPages);

        // Assert
        Assert.Equal(newTitle, book.Title);
        Assert.Equal(newDescription, book.Description);
        Assert.Equal(contentUrl, book.ContentUrl);
        Assert.Equal(totalPages, book.TotalPages);
        Assert.Equal(2, book.DomainEvents.Count);
        Assert.IsType<BookUpdatedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void SetPricing_WithValidPricePerPage_ShouldSetSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var pricePerPage = new Money(0.50m, "USD");

        // Act
        book.SetPricing(pricePerPage, null);

        // Assert
        Assert.Equal(pricePerPage, book.PricePerPage);
        Assert.Null(book.PricePerHour);
        Assert.Equal(2, book.DomainEvents.Count);
        Assert.IsType<BookPricingChangedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void SetPricing_WithValidPricePerHour_ShouldSetSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var pricePerHour = new Money(10.00m, "USD");

        // Act
        book.SetPricing(null, pricePerHour);

        // Assert
        Assert.Null(book.PricePerPage);
        Assert.Equal(pricePerHour, book.PricePerHour);
        Assert.Equal(2, book.DomainEvents.Count);
        Assert.IsType<BookPricingChangedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void SetPricing_WithBothPrices_ShouldSetSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var pricePerPage = new Money(0.25m, "USD");
        var pricePerHour = new Money(15.00m, "USD");

        // Act
        book.SetPricing(pricePerPage, pricePerHour);

        // Assert
        Assert.Equal(pricePerPage, book.PricePerPage);
        Assert.Equal(pricePerHour, book.PricePerHour);
    }

    [Fact]
    public void SetPricing_WithNoPrices_ShouldThrowArgumentException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.SetPricing(null, null));
    }

    [Fact]
    public void SetPricing_WithTooLowPricePerPage_ShouldThrowArgumentException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var invalidPrice = new Money(0.005m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.SetPricing(invalidPrice, null));
    }

    [Fact]
    public void SetPricing_WithTooHighPricePerPage_ShouldThrowArgumentException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var invalidPrice = new Money(1.50m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.SetPricing(invalidPrice, null));
    }

    [Fact]
    public void SetPricing_WithTooLowPricePerHour_ShouldThrowArgumentException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var invalidPrice = new Money(0.50m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.SetPricing(null, invalidPrice));
    }

    [Fact]
    public void SetPricing_WithTooHighPricePerHour_ShouldThrowArgumentException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var invalidPrice = new Money(60.00m, "USD");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.SetPricing(null, invalidPrice));
    }

    [Fact]
    public void Publish_WithValidBook_ShouldPublishSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 100);
        book.SetPricing(new Money(0.50m, "USD"), null);

        // Act
        book.Publish();

        // Assert
        Assert.True(book.IsPublished);
        Assert.NotNull(book.PublishedAt);
        Assert.Equal(BookStatus.Published, book.Status);
        Assert.Equal(4, book.DomainEvents.Count);
        Assert.IsType<BookPublishedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void Publish_WithoutContent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.SetPricing(new Money(0.50m, "USD"), null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => book.Publish());
    }

    [Fact]
    public void Publish_WithoutPages_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 0);
        book.SetPricing(new Money(0.50m, "USD"), null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => book.Publish());
    }

    [Fact]
    public void Publish_WithoutPricing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 100);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => book.Publish());
    }

    [Fact]
    public void Publish_AlreadyPublished_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 100);
        book.SetPricing(new Money(0.50m, "USD"), null);
        book.Publish();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => book.Publish());
    }

    [Fact]
    public void Unpublish_PublishedBook_ShouldUnpublishSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 100);
        book.SetPricing(new Money(0.50m, "USD"), null);
        book.Publish();

        // Act
        book.Unpublish();

        // Assert
        Assert.False(book.IsPublished);
        Assert.Equal(BookStatus.Draft, book.Status);
        Assert.Equal(5, book.DomainEvents.Count);
        Assert.IsType<BookUnpublishedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void Archive_ActiveBook_ShouldArchiveSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");

        // Act
        book.Archive();

        // Assert
        Assert.False(book.IsActive);
        Assert.Equal(BookStatus.Archived, book.Status);
        Assert.Equal(2, book.DomainEvents.Count);
        Assert.IsType<BookArchivedEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void Restore_ArchivedBook_ShouldRestoreSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.Archive();

        // Act
        book.Restore();

        // Assert
        Assert.True(book.IsActive);
        Assert.Equal(BookStatus.Draft, book.Status);
        Assert.Equal(3, book.DomainEvents.Count);
        Assert.IsType<BookRestoredEvent>(book.DomainEvents.Last());
    }

    [Fact]
    public void CanBeAccessedBy_Author_ShouldReturnTrue()
    {
        // Arrange
        var authorId = 1;
        var book = new Book("Title", "Description", authorId, "Category");

        // Act & Assert
        Assert.True(book.CanBeAccessedBy(authorId));
    }

    [Fact]
    public void CanBeAccessedBy_NonAuthorWithPublishedBook_ShouldReturnTrue()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        book.UpdateContent("Title", "Description", "https://example.com/content.pdf", 100);
        book.SetPricing(new Money(0.50m, "USD"), null);
        book.Publish();

        // Act & Assert
        Assert.True(book.CanBeAccessedBy(2));
    }

    [Fact]
    public void CanBeAccessedBy_NonAuthorWithDraftBook_ShouldReturnFalse()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");

        // Act & Assert
        Assert.False(book.CanBeAccessedBy(2));
    }

    [Fact]
    public void CalculatePageCharge_WithValidPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var pricePerPage = new Money(0.50m, "USD");
        book.SetPricing(pricePerPage, null);
        book.UpdateContent("Title", "Description", "content", 100);
        var pagesRead = 10;

        // Act
        var charge = book.CalculatePageCharge(pagesRead);

        // Assert
        Assert.Equal(new Money(5.00m, "USD"), charge);
    }

    [Fact]
    public void CalculateTimeCharge_WithValidTime_ShouldCalculateCorrectly()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var pricePerHour = new Money(10.00m, "USD");
        book.SetPricing(null, pricePerHour);
        var readingTime = TimeSpan.FromMinutes(30);

        // Act
        var charge = book.CalculateTimeCharge(readingTime);

        // Assert
        Assert.Equal(new Money(5.00m, "USD"), charge);
    }

    [Fact]
    public void UpdateRating_WithValidValues_ShouldUpdateSuccessfully()
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");
        var newRating = 4.5m;
        var newReviewCount = 10;

        // Act
        book.UpdateRating(newRating, newReviewCount);

        // Assert
        Assert.Equal(newRating, book.AverageRating);
        Assert.Equal(newReviewCount, book.ReviewCount);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void UpdateRating_WithInvalidRating_ShouldThrowArgumentException(decimal invalidRating)
    {
        // Arrange
        var book = new Book("Title", "Description", 1, "Category");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => book.UpdateRating(invalidRating, 1));
    }
}