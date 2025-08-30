namespace ByteBook.Domain.Events;

public class ReadingSessionEndedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public int PagesRead { get; }
    public int TimeSpentMinutes { get; }
    public DateTime OccurredOn { get; }

    public ReadingSessionEndedEvent(int readingId, int userId, int bookId, DateTime startTime, DateTime endTime, int pagesRead, int timeSpentMinutes)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        StartTime = startTime;
        EndTime = endTime;
        PagesRead = pagesRead;
        TimeSpentMinutes = timeSpentMinutes;
        OccurredOn = DateTime.UtcNow;
    }
}