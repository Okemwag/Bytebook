namespace ByteBook.Domain.Events;

public class UserLoginEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public DateTime OccurredOn { get; }

    public UserLoginEvent(int userId, string email, string? ipAddress = null, string? userAgent = null)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredOn = DateTime.UtcNow;
    }
}