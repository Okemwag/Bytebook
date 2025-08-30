namespace ByteBook.Domain.Events;

public class ReadingSessionResumedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public DateTime OccurredOn { get; }

    public ReadingSessionResumedEvent(int readingId, int userId, int bookId)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        OccurredOn = DateTime.UtcNow;
    }
}