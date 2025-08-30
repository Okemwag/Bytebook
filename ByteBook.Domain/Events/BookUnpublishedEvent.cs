namespace ByteBook.Domain.Events;

public class BookUnpublishedEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public DateTime OccurredOn { get; }

    public BookUnpublishedEvent(int bookId, string title, int authorId)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        OccurredOn = DateTime.UtcNow;
    }
}