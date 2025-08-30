namespace ByteBook.Domain.Events;

public class BookRestoredEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public DateTime OccurredOn { get; }

    public BookRestoredEvent(int bookId, string title, int authorId)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        OccurredOn = DateTime.UtcNow;
    }
}