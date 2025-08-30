using FluentValidation;

namespace ByteBook.Application.DTOs.Auth;

public class VerifyEmailDto
{
    public string Token { get; set; } = string.Empty;
}

public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required");
    }
}