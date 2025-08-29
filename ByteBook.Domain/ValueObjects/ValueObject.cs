using System.Text.RegularExpressions;

namespace ByteBook.Domain.ValueObjects;

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email() { } // EF Core

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value.ToLowerInvariant().Trim();
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

    public override bool Equals(object obj)
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