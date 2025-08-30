namespace ByteBook.Domain.Events;

public class UserDeactivatedEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserDeactivatedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}