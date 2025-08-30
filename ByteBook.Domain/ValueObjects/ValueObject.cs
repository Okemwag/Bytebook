using System.Text.RegularExpressions;

namespace ByteBook.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money() 
    { 
        Amount = 0;
        Currency = string.Empty;
    } // EF Core

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {Currency} and {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {Currency} and {other.Currency}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new ArgumentException("Cannot divide by zero", nameof(divisor));

        return new Money(Amount / divisor, Currency);
    }

    public bool IsZero => Amount == 0;

    public static Money Zero(string currency = "USD") => new(0, currency);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:C} {Currency}";

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);
    public static Money operator /(Money money, decimal divisor) => money.Divide(divisor);
}

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email() 
    { 
        Value = string.Empty;
    } // EF Core

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        var trimmedValue = value.Trim().ToLowerInvariant();
        
        if (!IsValidEmail(trimmedValue))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = trimmedValue;
    }

    private static bool IsValidEmail(string email)
    {
        const string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string email) => new(email);
}

public class UserProfile : ValueObject
{
    public string? Bio { get; private set; }
    public string? Website { get; private set; }
    public string? TwitterHandle { get; private set; }
    public string? LinkedInProfile { get; private set; }
    public string? AvatarUrl { get; private set; }
    public List<string> Interests { get; private set; }
    public string? Location { get; private set; }

    public UserProfile()
    {
        Interests = new List<string>();
    }

    public UserProfile(string? bio, string? website, string? twitterHandle, 
                      string? linkedInProfile, string? avatarUrl, List<string>? interests, string? location)
    {
        Bio = bio;
        Website = website;
        TwitterHandle = twitterHandle;
        LinkedInProfile = linkedInProfile;
        AvatarUrl = avatarUrl;
        Interests = interests ?? new List<string>();
        Location = location;
    }

    public UserProfile UpdateBio(string bio)
    {
        return new UserProfile(bio, Website, TwitterHandle, LinkedInProfile, AvatarUrl, Interests, Location);
    }

    public UserProfile UpdateWebsite(string website)
    {
        return new UserProfile(Bio, website, TwitterHandle, LinkedInProfile, AvatarUrl, Interests, Location);
    }

    public UserProfile UpdateSocialProfiles(string? twitterHandle, string? linkedInProfile)
    {
        return new UserProfile(Bio, Website, twitterHandle, linkedInProfile, AvatarUrl, Interests, Location);
    }

    public UserProfile UpdateInterests(List<string> interests)
    {
        return new UserProfile(Bio, Website, TwitterHandle, LinkedInProfile, AvatarUrl, interests, Location);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bio ?? string.Empty;
        yield return Website ?? string.Empty;
        yield return TwitterHandle ?? string.Empty;
        yield return LinkedInProfile ?? string.Empty;
        yield return AvatarUrl ?? string.Empty;
        yield return Location ?? string.Empty;
        
        foreach (var interest in Interests.OrderBy(x => x))
        {
            yield return interest;
        }
    }
}

public abstract class ValueObject
{
    protected static bool EqualOperator(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }
        return ReferenceEquals(left, null) || left.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
    {
        return !(EqualOperator(left, right));
    }

    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}