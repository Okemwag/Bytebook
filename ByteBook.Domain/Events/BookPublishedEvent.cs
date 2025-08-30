namespace ByteBook.Domain.Events;

public class BookPublishedEvent : IDomainEvent
{
    public int BookId { get; }
    public string Title { get; }
    public int AuthorId { get; }
    public DateTime PublishedAt { get; }
    public DateTime OccurredOn { get; }

    public BookPublishedEvent(int bookId, string title, int authorId, DateTime publishedAt)
    {
        BookId = bookId;
        Title = title;
        AuthorId = authorId;
        PublishedAt = publishedAt;
        OccurredOn = DateTime.UtcNow;
    }
}