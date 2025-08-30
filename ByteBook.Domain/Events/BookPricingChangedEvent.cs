using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Events;

public class BookPricingChangedEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public Money? OldPricePerPage { get; }
    public Money? OldPricePerHour { get; }
    public Money? NewPricePerPage { get; }
    public Money? NewPricePerHour { get; }
    public DateTime OccurredOn { get; }

    public BookPricingChangedEvent(int bookId, string title, int authorId, 
        Money? oldPricePerPage, Money? oldPricePerHour, 
        Money? newPricePerPage, Money? newPricePerHour)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        OldPricePerPage = oldPricePerPage;
        OldPricePerHour = oldPricePerHour;
        NewPricePerPage = newPricePerPage;
        NewPricePerHour = newPricePerHour;
        OccurredOn = DateTime.UtcNow;
    }
}