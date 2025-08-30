namespace ByteBook.Domain.Events;

public class ReadingSessionTerminatedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }

    public ReadingSessionTerminatedEvent(int readingId, int userId, int bookId, string reason)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }
}