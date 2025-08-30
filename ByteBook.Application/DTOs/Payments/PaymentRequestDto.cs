using FluentValidation;

namespace ByteBook.Application.DTOs.Payments;

public class PaymentRequestDto
{
    public int BookId { get; set; }
    public string PaymentType { get; set; } = string.Empty; // "PerPage" or "PerHour"
    public string Provider { get; set; } = string.Empty; // "Stripe", "PayPal", "MPesa"
    public int? PagesRead { get; set; }
    public int? TimeSpentMinutes { get; set; }
    public string? PaymentMethodId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class PaymentResultDto
{
    public bool IsSuccess { get; set; }
    public string? PaymentId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? PaymentUrl { get; set; } // For redirect-based payments
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RefundRequestDto
{
    public int PaymentId { get; set; }
    public decimal? Amount { get; set; } // Partial refund if specified
    public string Reason { get; set; } = string.Empty;
}

public class RefundResultDto
{
    public bool IsSuccess { get; set; }
    public string? RefundId { get; set; }
    public string? ExternalRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class PaymentRequestDtoValidator : AbstractValidator<PaymentRequestDto>
{
    public PaymentRequestDtoValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Valid book ID is required");

        RuleFor(x => x.PaymentType)
            .NotEmpty().WithMessage("Payment type is required")
            .Must(BeValidPaymentType).WithMessage("Payment type must be 'PerPage' or 'PerHour'");

        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Payment provider is required")
            .Must(BeValidProvider).WithMessage("Payment provider must be 'Stripe', 'PayPal', or 'MPesa'");

        RuleFor(x => x.PagesRead)
            .GreaterThan(0).WithMessage("Pages read must be greater than 0")
            .When(x => x.PaymentType == "PerPage");

        RuleFor(x => x.TimeSpentMinutes)
            .GreaterThan(0).WithMessage("Time spent must be greater than 0")
            .When(x => x.PaymentType == "PerHour");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty().WithMessage("Payment method ID is required for Stripe payments")
            .When(x => x.Provider == "Stripe");
    }

    private bool BeValidPaymentType(string paymentType)
    {
        return paymentType == "PerPage" || paymentType == "PerHour";
    }

    private bool BeValidProvider(string provider)
    {
        var validProviders = new[] { "Stripe", "PayPal", "MPesa" };
        return validProviders.Contains(provider);
    }
}

public class RefundRequestDtoValidator : AbstractValidator<RefundRequestDto>
{
    public RefundRequestDtoValidator()
    {
        RuleFor(x => x.PaymentId)
            .GreaterThan(0).WithMessage("Valid payment ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than 0")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Refund reason is required")
            .MaximumLength(500).WithMessage("Refund reason cannot exceed 500 characters");
    }
}