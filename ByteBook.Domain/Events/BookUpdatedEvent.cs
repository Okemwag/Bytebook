namespace ByteBook.Domain.Events;

public class BookUpdatedEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public bool TitleChanged { get; }
    public DateTime OccurredOn { get; }

    public BookUpdatedEvent(int bookId, string title, int authorId, bool titleChanged)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        TitleChanged = titleChanged;
        OccurredOn = DateTime.UtcNow;
    }
}