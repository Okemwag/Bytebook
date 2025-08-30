using ByteBook.Domain.ValueObjects;

namespace ByteBook.UnitTests.Domain.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Email_WithValidEmail_ShouldCreateSuccessfully()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var email = new Email(validEmail);

        // Assert
        Assert.Equal(validEmail.ToLowerInvariant(), email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Email_WithEmptyOrWhitespace_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test.example.com")]
    [InlineData("test@.com")]
    [InlineData("test@example.")]
    public void Email_WithInvalidFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Email(invalidEmail));
    }

    [Fact]
    public void Email_ShouldNormalizeToLowerCase()
    {
        // Arrange
        var upperCaseEmail = "TEST@EXAMPLE.COM";

        // Act
        var email = new Email(upperCaseEmail);

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Email_ShouldTrimWhitespace()
    {
        // Arrange
        var emailWithWhitespace = "  test@example.com  ";

        // Act
        var email = new Email(emailWithWhitespace);

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Email_ImplicitConversionToString_ShouldWork()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        string emailString = email;

        // Assert
        Assert.Equal("test@example.com", emailString);
    }

    [Fact]
    public void Email_ImplicitConversionFromString_ShouldWork()
    {
        // Arrange
        string emailString = "test@example.com";

        // Act
        Email email = emailString;

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Email_EqualityComparison_ShouldWork()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");
        var email3 = new Email("different@example.com");

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.NotEqual(email1, email3);
        Assert.True(email1.Equals(email2));
        Assert.False(email1.Equals(email3));
    }
}