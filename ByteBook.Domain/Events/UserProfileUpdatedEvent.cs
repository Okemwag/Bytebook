namespace ByteBook.Domain.Events;

public class UserProfileUpdatedEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserProfileUpdatedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}