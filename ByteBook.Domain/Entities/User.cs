using ByteBook.Domain.Events;
using ByteBook.Domain.ValueObjects;

namespace ByteBook.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public UserProfile Profile { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public string? ResetPasswordToken { get; private set; }
    public DateTime? ResetPasswordTokenExpiry { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime LastLoginAt { get; private set; }
    
    // Navigation properties - will be uncommented when entities are created
    // public ICollection<Book> AuthoredBooks { get; private set; } = new List<Book>();
    // public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    // public ICollection<Referral> Referrals { get; private set; } = new List<Referral>();
    // public ICollection<Reading> Readings { get; private set; } = new List<Reading>();

    private User() 
    { 
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = new Email("placeholder@example.com");
        PasswordHash = string.Empty;
        Profile = new UserProfile();
    } // EF Core

    public User(string firstName, string lastName, Email email, string passwordHash, UserRole role = UserRole.Reader)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Profile = new UserProfile();
        IsEmailVerified = false;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        
        GenerateEmailVerificationToken();
        AddDomainEvent(new UserRegisteredEvent(Id, Email.Value, FirstName, LastName));
    }

    public void VerifyEmail(string token)
    {
        if (IsEmailVerified)
            throw new InvalidOperationException("Email is already verified");
            
        if (EmailVerificationToken != token)
            throw new InvalidOperationException("Invalid verification token");
            
        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;
        EmailVerificationToken = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserEmailVerifiedEvent(Id, Email.Value));
    }

    public void GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Guid.NewGuid().ToString("N");
    }

    public void GeneratePasswordResetToken()
    {
        ResetPasswordToken = Guid.NewGuid().ToString("N");
        ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(24);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetPassword(string token, string newPasswordHash)
    {
        if (ResetPasswordToken != token)
            throw new InvalidOperationException("Invalid reset token");
            
        if (ResetPasswordTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Reset token has expired");
            
        PasswordHash = newPasswordHash;
        ResetPasswordToken = null;
        ResetPasswordTokenExpiry = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserPasswordResetEvent(Id, Email.Value));
    }

    public void UpdateProfile(string firstName, string lastName, UserProfile profile)
    {
        FirstName = firstName;
        LastName = lastName;
        Profile = profile;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserProfileUpdatedEvent(Id, Email.Value));
    }

    public void UpdateLastLogin(string? ipAddress = null, string? userAgent = null)
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserLoginEvent(Id, Email.Value, ipAddress, userAgent));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserDeactivatedEvent(Id, Email.Value));
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new UserActivatedEvent(Id, Email.Value));
    }

    public string GetFullName() => $"{FirstName} {LastName}";
}

public enum UserRole
{
    Reader = 1,
    Author = 2,
    Admin = 3
}