using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public int AuthorId { get; private set; }
    public string Category { get; private set; }
    public BookStatus Status { get; private set; }
    public string? ContentUrl { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public int TotalPages { get; private set; }
    public Money? PricePerPage { get; private set; }
    public Money? PricePerHour { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? Tags { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties - will be uncommented when entities are created
    // public User Author { get; private set; }
    // public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    // public ICollection<Reading> Readings { get; private set; } = new List<Reading>();
    // public ICollection<Review> Reviews { get; private set; } = new List<Review>();

    private Book()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
    } // EF Core

    public Book(string title, string description, int authorId, string category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        
        if (authorId <= 0)
            throw new ArgumentException("Author ID must be positive", nameof(authorId));
        
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        Title = title.Trim();
        Description = description.Trim();
        AuthorId = authorId;
        Category = category.Trim();
        Status = BookStatus.Draft;
        TotalPages = 0;
        IsPublished = false;
        IsActive = true;
        AverageRating = 0;
        ReviewCount = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookCreatedEvent(Id, title, authorId));
    }

    public void UpdateContent(string title, string description, string? contentUrl, int totalPages)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        
        if (totalPages < 0)
            throw new ArgumentException("Total pages cannot be negative", nameof(totalPages));

        var oldTitle = Title;
        Title = title.Trim();
        Description = description.Trim();
        ContentUrl = contentUrl;
        TotalPages = totalPages;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookUpdatedEvent(Id, Title, AuthorId, oldTitle != Title));
    }

    public void SetPricing(Money? pricePerPage, Money? pricePerHour)
    {
        if (pricePerPage != null && pricePerPage.Amount < 0.01m)
            throw new ArgumentException("Price per page must be at least $0.01", nameof(pricePerPage));
        
        if (pricePerPage != null && pricePerPage.Amount > 1.00m)
            throw new ArgumentException("Price per page cannot exceed $1.00", nameof(pricePerPage));
        
        if (pricePerHour != null && pricePerHour.Amount < 1.00m)
            throw new ArgumentException("Price per hour must be at least $1.00", nameof(pricePerHour));
        
        if (pricePerHour != null && pricePerHour.Amount > 50.00m)
            throw new ArgumentException("Price per hour cannot exceed $50.00", nameof(pricePerHour));

        if (pricePerPage == null && pricePerHour == null)
            throw new ArgumentException("At least one pricing model must be set");

        var oldPricePerPage = PricePerPage;
        var oldPricePerHour = PricePerHour;
        
        PricePerPage = pricePerPage;
        PricePerHour = pricePerHour;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookPricingChangedEvent(Id, Title, AuthorId, oldPricePerPage, oldPricePerHour, pricePerPage, pricePerHour));
    }

    public void SetCoverImage(string coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl))
            throw new ArgumentException("Cover image URL cannot be empty", nameof(coverImageUrl));

        CoverImageUrl = coverImageUrl;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookUpdatedEvent(Id, Title, AuthorId, false));
    }

    public void SetTags(string tags)
    {
        Tags = tags?.Trim();
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookUpdatedEvent(Id, Title, AuthorId, false));
    }

    public void Publish()
    {
        if (IsPublished)
            throw new InvalidOperationException("Book is already published");
        
        if (string.IsNullOrWhiteSpace(ContentUrl))
            throw new InvalidOperationException("Cannot publish book without content");
        
        if (TotalPages <= 0)
            throw new InvalidOperationException("Cannot publish book with no pages");
        
        if (PricePerPage == null && PricePerHour == null)
            throw new InvalidOperationException("Cannot publish book without pricing");

        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        Status = BookStatus.Published;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookPublishedEvent(Id, Title, AuthorId, PublishedAt.Value));
    }

    public void Unpublish()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Book is not published");

        IsPublished = false;
        Status = BookStatus.Draft;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookUnpublishedEvent(Id, Title, AuthorId));
    }

    public void Archive()
    {
        if (!IsActive)
            throw new InvalidOperationException("Book is already archived");

        IsActive = false;
        Status = BookStatus.Archived;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookArchivedEvent(Id, Title, AuthorId));
    }

    public void Restore()
    {
        if (IsActive)
            throw new InvalidOperationException("Book is already active");

        IsActive = true;
        Status = IsPublished ? BookStatus.Published : BookStatus.Draft;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BookRestoredEvent(Id, Title, AuthorId));
    }

    public void UpdateRating(decimal newRating, int newReviewCount)
    {
        if (newRating < 0 || newRating > 5)
            throw new ArgumentException("Rating must be between 0 and 5", nameof(newRating));
        
        if (newReviewCount < 0)
            throw new ArgumentException("Review count cannot be negative", nameof(newReviewCount));

        AverageRating = newRating;
        ReviewCount = newReviewCount;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanBeAccessedBy(int userId)
    {
        // Author can always access their own books
        if (AuthorId == userId)
            return true;

        // Only published and active books can be accessed by others
        return IsPublished && IsActive;
    }

    public Money CalculatePageCharge(int pagesRead)
    {
        if (PricePerPage == null)
            throw new InvalidOperationException("Book does not support per-page pricing");
        
        if (pagesRead < 0)
            throw new ArgumentException("Pages read cannot be negative", nameof(pagesRead));
        
        if (pagesRead > TotalPages)
            throw new ArgumentException("Pages read cannot exceed total pages", nameof(pagesRead));

        return PricePerPage * pagesRead;
    }

    public Money CalculateTimeCharge(TimeSpan readingTime)
    {
        if (PricePerHour == null)
            throw new InvalidOperationException("Book does not support per-hour pricing");
        
        if (readingTime < TimeSpan.Zero)
            throw new ArgumentException("Reading time cannot be negative", nameof(readingTime));

        var hours = (decimal)readingTime.TotalHours;
        return PricePerHour * hours;
    }
}

public enum BookStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}