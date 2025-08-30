namespace ByteBook.Domain.Events;

public class ReadingProgressUpdatedEvent : IDomainEvent
{
    public int ReadingId { get; }
    public int UserId { get; }
    public int BookId { get; }
    public int CurrentPage { get; }
    public int TotalPagesRead { get; }
    public int PreviousPagesRead { get; }
    public DateTime OccurredOn { get; }

    public ReadingProgressUpdatedEvent(int readingId, int userId, int bookId, int currentPage, int totalPagesRead, int previousPagesRead)
    {
        ReadingId = readingId;
        UserId = userId;
        BookId = bookId;
        CurrentPage = currentPage;
        TotalPagesRead = totalPagesRead;
        PreviousPagesRead = previousPagesRead;
        OccurredOn = DateTime.UtcNow;
    }
}