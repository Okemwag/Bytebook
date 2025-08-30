using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Entities;

public class Reading : BaseEntity
{
    public int UserId { get; private set; }
    public int BookId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public int PagesRead { get; private set; }
    public int TimeSpentMinutes { get; private set; }
    public int LastPageRead { get; private set; }
    public bool IsCompleted { get; private set; }
    public ReadingStatus Status { get; private set; }
    public Money? ChargedAmount { get; private set; }
    public PaymentType? ChargeType { get; private set; }

    // Navigation properties - will be uncommented when entities are created
    // public User User { get; private set; }
    // public Book Book { get; private set; }
    // public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    private Reading()
    {
    } // EF Core

    public Reading(int userId, int bookId)
    {
        if (userId <= 0)
            throw new ArgumentException("User ID must be positive", nameof(userId));
        
        if (bookId <= 0)
            throw new ArgumentException("Book ID must be positive", nameof(bookId));

        UserId = userId;
        BookId = bookId;
        StartTime = DateTime.UtcNow;
        PagesRead = 0;
        TimeSpentMinutes = 0;
        LastPageRead = 0;
        IsCompleted = false;
        Status = ReadingStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingSessionStartedEvent(Id, userId, bookId, StartTime));
    }

    public void UpdateProgress(int currentPage, int totalPagesRead)
    {
        if (Status != ReadingStatus.Active)
            throw new InvalidOperationException($"Cannot update progress for {Status} reading session");
        
        if (currentPage < 0)
            throw new ArgumentException("Current page cannot be negative", nameof(currentPage));
        
        if (totalPagesRead < 0)
            throw new ArgumentException("Total pages read cannot be negative", nameof(totalPagesRead));
        
        if (totalPagesRead < PagesRead)
            throw new ArgumentException("Total pages read cannot decrease", nameof(totalPagesRead));

        var oldPagesRead = PagesRead;
        LastPageRead = currentPage;
        PagesRead = totalPagesRead;
        UpdatedAt = DateTime.UtcNow;

        if (totalPagesRead > oldPagesRead)
        {
            AddDomainEvent(new ReadingProgressUpdatedEvent(Id, UserId, BookId, currentPage, totalPagesRead, oldPagesRead));
        }
    }

    public void EndSession()
    {
        if (Status != ReadingStatus.Active)
            throw new InvalidOperationException($"Cannot end {Status} reading session");
        
        if (EndTime.HasValue)
            throw new InvalidOperationException("Reading session is already ended");

        EndTime = DateTime.UtcNow;
        TimeSpentMinutes = (int)(EndTime.Value - StartTime).TotalMinutes;
        Status = ReadingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingSessionEndedEvent(Id, UserId, BookId, StartTime, EndTime.Value, PagesRead, TimeSpentMinutes));
    }

    public void PauseSession()
    {
        if (Status != ReadingStatus.Active)
            throw new InvalidOperationException($"Cannot pause {Status} reading session");

        Status = ReadingStatus.Paused;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingSessionPausedEvent(Id, UserId, BookId));
    }

    public void ResumeSession()
    {
        if (Status != ReadingStatus.Paused)
            throw new InvalidOperationException($"Cannot resume {Status} reading session");

        Status = ReadingStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingSessionResumedEvent(Id, UserId, BookId));
    }

    public void MarkAsCompleted()
    {
        if (IsCompleted)
            throw new InvalidOperationException("Reading session is already completed");

        if (Status == ReadingStatus.Active)
        {
            EndSession();
        }

        IsCompleted = true;
        Status = ReadingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingCompletedEvent(Id, UserId, BookId, PagesRead, TimeSpentMinutes));
    }

    public Money CalculatePageCharges(Money pricePerPage, int totalBookPages)
    {
        if (pricePerPage == null)
            throw new ArgumentException("Price per page cannot be null", nameof(pricePerPage));
        
        if (totalBookPages <= 0)
            throw new ArgumentException("Total book pages must be positive", nameof(totalBookPages));
        
        if (PagesRead > totalBookPages)
            throw new InvalidOperationException("Pages read cannot exceed total book pages");

        return pricePerPage.Multiply(PagesRead);
    }

    public Money CalculateTimeCharges(Money pricePerHour)
    {
        if (pricePerHour == null)
            throw new ArgumentException("Price per hour cannot be null", nameof(pricePerHour));

        var hours = TimeSpentMinutes / 60.0m;
        return pricePerHour.Multiply(hours);
    }

    public void RecordCharge(Money amount, PaymentType chargeType)
    {
        if (amount == null || amount.IsZero)
            throw new ArgumentException("Charge amount must be greater than zero", nameof(amount));

        ChargedAmount = ChargedAmount == null ? amount : ChargedAmount.Add(amount);
        ChargeType = chargeType;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingChargedEvent(Id, UserId, BookId, amount, chargeType));
    }

    public TimeSpan GetCurrentDuration()
    {
        var endTime = EndTime ?? DateTime.UtcNow;
        return endTime - StartTime;
    }

    public decimal GetReadingRate()
    {
        if (TimeSpentMinutes == 0)
            return 0;

        return (decimal)PagesRead / (TimeSpentMinutes / 60.0m);
    }

    public bool HasExceededTimeLimit(int maxMinutes)
    {
        if (maxMinutes <= 0)
            return false;

        return GetCurrentDuration().TotalMinutes > maxMinutes;
    }

    public void ForceEnd(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty", nameof(reason));

        if (Status == ReadingStatus.Active || Status == ReadingStatus.Paused)
        {
            EndTime = DateTime.UtcNow;
            TimeSpentMinutes = (int)(EndTime.Value - StartTime).TotalMinutes;
        }

        Status = ReadingStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ReadingSessionTerminatedEvent(Id, UserId, BookId, reason));
    }
}

public enum ReadingStatus
{
    Active = 1,
    Paused = 2,
    Completed = 3,
    Terminated = 4
}