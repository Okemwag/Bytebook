namespace ByteBook.Domain.Events;

public class UserPasswordResetEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserPasswordResetEvent(int userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}