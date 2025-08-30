using FluentValidation;

namespace ByteBook.Application.DTOs.Auth;

public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required");
    }
}

public class VerifyEmailDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}