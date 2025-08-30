namespace ByteBook.Domain.Events;

public class ReadingCompletedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public int PagesRead { get; }
    public int TimeSpentMinutes { get; }
    public DateTime OccurredOn { get; }

    public ReadingCompletedEvent(int readingId, int userId, int bookId, int pagesRead, int timeSpentMinutes)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        PagesRead = pagesRead;
        TimeSpentMinutes = timeSpentMinutes;
        OccurredOn = DateTime.UtcNow;
    }
}