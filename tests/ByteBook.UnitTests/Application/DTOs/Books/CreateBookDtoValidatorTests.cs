using ByteBook.Application.DTOs.Books;
using FluentValidation;
using Xunit;

namespace ByteBook.UnitTests.Application.DTOs.Books;

public class CreateBookDtoValidatorTests
{
    private readonly CreateBookDtoValidator _validator;

    public CreateBookDtoValidatorTests()
    {
        _validator = new CreateBookDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var dto = new CreateBookDto { Title = "" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Too_Short()
    {
        var dto = new CreateBookDto { Title = "AB" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Too_Long()
    {
        var dto = new CreateBookDto { Title = new string('A', 201) };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Title));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Title_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.Title = "Valid Book Title";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Title));
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Empty()
    {
        var dto = new CreateBookDto { Description = "" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Description));
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Too_Short()
    {
        var dto = new CreateBookDto { Description = "Short" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Description));
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Too_Long()
    {
        var dto = new CreateBookDto { Description = new string('A', 2001) };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Description));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Description_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.Description = "This is a valid book description that meets the length requirements.";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateBookDto.Description));
    }

    [Fact]
    public void Should_Have_Error_When_PricePerPage_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.PricePerPage = 0;
        dto.PricePerHour = null; // Remove other pricing to test this specific case
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerPage));
    }

    [Fact]
    public void Should_Have_Error_When_PricePerPage_Exceeds_Maximum()
    {
        var dto = CreateValidDto();
        dto.PricePerPage = 1.01m;
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerPage));
    }

    [Fact]
    public void Should_Not_Have_Error_When_PricePerPage_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.PricePerPage = 0.50m;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerPage));
    }

    [Fact]
    public void Should_Have_Error_When_PricePerHour_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.PricePerHour = 0;
        dto.PricePerPage = null; // Remove other pricing to test this specific case
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerHour));
    }

    [Fact]
    public void Should_Have_Error_When_PricePerHour_Exceeds_Maximum()
    {
        var dto = CreateValidDto();
        dto.PricePerHour = 50.01m;
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerHour));
    }

    [Fact]
    public void Should_Not_Have_Error_When_PricePerHour_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.PricePerHour = 25.00m;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(CreateBookDto.PricePerHour));
    }

    [Fact]
    public void Should_Have_Error_When_No_Pricing_Model_Is_Specified()
    {
        var dto = new CreateBookDto 
        { 
            Title = "Valid Title",
            Description = "Valid description that meets requirements",
            Category = "Fiction",
            Content = "Some content"
        };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        // This should have a validation error for the entire object
        Assert.Contains(result.Errors, e => string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "");
    }

    [Fact]
    public void Should_Not_Have_Error_When_At_Least_One_Pricing_Model_Is_Specified()
    {
        var dto = new CreateBookDto 
        { 
            Title = "Valid Title",
            Description = "Valid description that meets requirements",
            Category = "Fiction",
            Content = "Some content",
            PricePerPage = 0.10m
        };
        var result = _validator.Validate(dto);
        // Should not have the "at least one pricing model" error
        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage.Contains("At least one pricing model"));
    }

    [Fact]
    public void Should_Have_Error_When_No_Content_Is_Provided()
    {
        var dto = new CreateBookDto 
        { 
            Title = "Valid Title",
            Description = "Valid description that meets requirements",
            Category = "Fiction",
            PricePerPage = 0.10m
        };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        // This should have a validation error for content requirement
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("content"));
    }

    [Fact]
    public void Should_Not_Have_Error_When_Content_Is_Provided()
    {
        var dto = new CreateBookDto 
        { 
            Title = "Valid Title",
            Description = "Valid description that meets requirements",
            Category = "Fiction",
            Content = "Book content here",
            PricePerPage = 0.10m
        };
        var result = _validator.Validate(dto);
        // Should not have the content requirement error
        Assert.DoesNotContain(result.Errors, e => e.ErrorMessage.Contains("content"));
    }

    private CreateBookDto CreateValidDto()
    {
        return new CreateBookDto
        {
            Title = "Valid Book Title",
            Description = "This is a valid book description that meets the length requirements.",
            Category = "Fiction",
            Content = "Some book content",
            PricePerPage = 0.10m
        };
    }
}