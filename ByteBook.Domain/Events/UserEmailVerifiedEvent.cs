namespace ByteBook.Domain.Events;

public class UserEmailVerifiedEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserEmailVerifiedEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}