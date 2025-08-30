namespace ByteBook.Domain.Events;

public class BookCreatedEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public DateTime OccurredOn { get; }

    public BookCreatedEvent(int bookId, string title, int authorId)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        OccurredOn = DateTime.UtcNow;
    }
}