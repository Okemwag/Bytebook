using ByteBook.Application.DTOs.Auth;
using FluentValidation;
using Xunit;

namespace ByteBook.UnitTests.Application.DTOs.Auth;

public class RegisterUserDtoValidatorTests
{
    private readonly RegisterUserDtoValidator _validator;

    public RegisterUserDtoValidatorTests()
    {
        _validator = new RegisterUserDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var dto = new RegisterUserDto { FirstName = "" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.FirstName));
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Too_Short()
    {
        var dto = new RegisterUserDto { FirstName = "A" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.FirstName));
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Too_Long()
    {
        var dto = new RegisterUserDto { FirstName = new string('A', 51) };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.FirstName));
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Contains_Numbers()
    {
        var dto = new RegisterUserDto { FirstName = "John123" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.FirstName));
    }

    [Fact]
    public void Should_Not_Have_Error_When_FirstName_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.FirstName = "John";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.FirstName));
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        var dto = new RegisterUserDto { Email = "invalid-email" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Email));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Email_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.Email = "john@example.com";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Email));
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Weak()
    {
        var dto = new RegisterUserDto { Password = "weak" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Password));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Password_Is_Strong()
    {
        var dto = CreateValidDto();
        dto.Password = "StrongPass123!";
        dto.ConfirmPassword = "StrongPass123!";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Password));
    }

    [Fact]
    public void Should_Have_Error_When_Passwords_Do_Not_Match()
    {
        var dto = CreateValidDto();
        dto.Password = "StrongPass123!";
        dto.ConfirmPassword = "DifferentPass123!";
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.ConfirmPassword));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Passwords_Match()
    {
        var dto = CreateValidDto();
        dto.Password = "StrongPass123!";
        dto.ConfirmPassword = "StrongPass123!";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.ConfirmPassword));
    }

    [Fact]
    public void Should_Have_Error_When_Bio_Is_Too_Long()
    {
        var dto = CreateValidDto();
        dto.Bio = new string('A', 501);
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Bio));
    }

    [Fact]
    public void Should_Have_Error_When_Website_Is_Invalid_Url()
    {
        var dto = CreateValidDto();
        dto.Website = "not-a-url";
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Website));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Website_Is_Valid_Url()
    {
        var dto = CreateValidDto();
        dto.Website = "https://example.com";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.Website));
    }

    [Fact]
    public void Should_Have_Error_When_SocialLinks_Contains_Invalid_Platform()
    {
        var dto = CreateValidDto();
        dto.SocialLinks = new Dictionary<string, string> 
        { 
            { "invalid-platform", "https://example.com" } 
        };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.SocialLinks));
    }

    [Fact]
    public void Should_Not_Have_Error_When_SocialLinks_Are_Valid()
    {
        var dto = CreateValidDto();
        dto.SocialLinks = new Dictionary<string, string> 
        { 
            { "twitter", "https://twitter.com/user" },
            { "linkedin", "https://linkedin.com/in/user" }
        };
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(RegisterUserDto.SocialLinks));
    }

    private RegisterUserDto CreateValidDto()
    {
        return new RegisterUserDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "StrongPass123!",
            ConfirmPassword = "StrongPass123!"
        };
    }
}