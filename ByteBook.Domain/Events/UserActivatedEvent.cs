namespace ByteBook.Domain.Events;

public class UserActivatedEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserActivatedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}