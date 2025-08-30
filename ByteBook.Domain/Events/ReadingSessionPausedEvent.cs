namespace ByteBook.Domain.Events;

public class ReadingSessionPausedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public DateTime OccurredOn { get; }

    public ReadingSessionPausedEvent(int readingId, int userId, int bookId)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        OccurredOn = DateTime.UtcNow;
    }
}