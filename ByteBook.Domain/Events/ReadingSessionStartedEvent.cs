namespace ByteBook.Domain.Events;

public class ReadingSessionStartedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public DateTime StartTime { get; }
    public DateTime OccurredOn { get; }

    public ReadingSessionStartedEvent(int readingId, int userId, int bookId, DateTime startTime)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        StartTime = startTime;
        OccurredOn = DateTime.UtcNow;
    }
}