namespace ByteBook.Domain.Events;

public class UserRegisteredEvent : IDomainEvent
{
    public int UserId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public DateTime OccurredOn { get; }

    public UserRegisteredEvent(int userId, string email, string firstName, string lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        OccurredOn = DateTime.UtcNow;
    }
}