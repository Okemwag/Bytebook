using ByteBook.Application.DTOs.Payments;
using FluentValidation;
using Xunit;

namespace ByteBook.UnitTests.Application.DTOs.Payments;

public class PaymentRequestDtoValidatorTests
{
    private readonly PaymentRequestDtoValidator _validator;

    public PaymentRequestDtoValidatorTests()
    {
        _validator = new PaymentRequestDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_BookId_Is_Zero()
    {
        var dto = new PaymentRequestDto { BookId = 0 };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.BookId));
    }

    [Fact]
    public void Should_Have_Error_When_BookId_Is_Negative()
    {
        var dto = new PaymentRequestDto { BookId = -1 };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.BookId));
    }

    [Fact]
    public void Should_Not_Have_Error_When_BookId_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.BookId = 1;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.BookId));
    }

    [Fact]
    public void Should_Have_Error_When_PaymentType_Is_Empty()
    {
        var dto = new PaymentRequestDto { PaymentType = "" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentType));
    }

    [Fact]
    public void Should_Have_Error_When_PaymentType_Is_Invalid()
    {
        var dto = new PaymentRequestDto { PaymentType = "InvalidType" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentType));
    }

    [Theory]
    [InlineData("PerPage")]
    [InlineData("PerHour")]
    public void Should_Not_Have_Error_When_PaymentType_Is_Valid(string paymentType)
    {
        var dto = CreateValidDto();
        dto.PaymentType = paymentType;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentType));
    }

    [Fact]
    public void Should_Have_Error_When_Provider_Is_Empty()
    {
        var dto = new PaymentRequestDto { Provider = "" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.Provider));
    }

    [Fact]
    public void Should_Have_Error_When_Provider_Is_Invalid()
    {
        var dto = new PaymentRequestDto { Provider = "InvalidProvider" };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.Provider));
    }

    [Theory]
    [InlineData("Stripe")]
    [InlineData("PayPal")]
    [InlineData("MPesa")]
    public void Should_Not_Have_Error_When_Provider_Is_Valid(string provider)
    {
        var dto = CreateValidDto();
        dto.Provider = provider;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.Provider));
    }

    [Fact]
    public void Should_Have_Error_When_PagesRead_Is_Zero_For_PerPage_Payment()
    {
        var dto = CreateValidDto();
        dto.PaymentType = "PerPage";
        dto.PagesRead = 0;
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PagesRead));
    }

    [Fact]
    public void Should_Not_Have_Error_When_PagesRead_Is_Valid_For_PerPage_Payment()
    {
        var dto = CreateValidDto();
        dto.PaymentType = "PerPage";
        dto.PagesRead = 5;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PagesRead));
    }

    [Fact]
    public void Should_Have_Error_When_TimeSpentMinutes_Is_Zero_For_PerHour_Payment()
    {
        var dto = CreateValidDto();
        dto.PaymentType = "PerHour";
        dto.TimeSpentMinutes = 0;
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.TimeSpentMinutes));
    }

    [Fact]
    public void Should_Not_Have_Error_When_TimeSpentMinutes_Is_Valid_For_PerHour_Payment()
    {
        var dto = CreateValidDto();
        dto.PaymentType = "PerHour";
        dto.TimeSpentMinutes = 30;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.TimeSpentMinutes));
    }

    [Fact]
    public void Should_Have_Error_When_PaymentMethodId_Is_Empty_For_Stripe()
    {
        var dto = CreateValidDto();
        dto.Provider = "Stripe";
        dto.PaymentMethodId = "";
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentMethodId));
    }

    [Fact]
    public void Should_Not_Have_Error_When_PaymentMethodId_Is_Provided_For_Stripe()
    {
        var dto = CreateValidDto();
        dto.Provider = "Stripe";
        dto.PaymentMethodId = "pm_1234567890";
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentMethodId));
    }

    [Fact]
    public void Should_Not_Require_PaymentMethodId_For_Non_Stripe_Providers()
    {
        var dto = CreateValidDto();
        dto.Provider = "PayPal";
        dto.PaymentMethodId = null;
        var result = _validator.Validate(dto);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PaymentRequestDto.PaymentMethodId));
    }

    private PaymentRequestDto CreateValidDto()
    {
        return new PaymentRequestDto
        {
            BookId = 1,
            PaymentType = "PerPage",
            Provider = "Stripe",
            PagesRead = 5,
            PaymentMethodId = "pm_1234567890"
        };
    }
}