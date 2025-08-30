using FluentValidation;

namespace ByteBook.Application.DTOs.Reading;

public class ReadingSessionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int PagesRead { get; set; }
    public int TimeSpentMinutes { get; set; }
    public int LastPageRead { get; set; }
    public bool IsCompleted { get; set; }
    public decimal? EstimatedCharge { get; set; }
    public string? ChargeType { get; set; }
}

public class StartReadingSessionDto
{
    public int BookId { get; set; }
    public string PaymentType { get; set; } = string.Empty; // "PerPage" or "PerHour"
}

public class UpdateReadingProgressDto
{
    public int SessionId { get; set; }
    public int CurrentPage { get; set; }
    public int TimeSpentMinutes { get; set; }
}

public class EndReadingSessionDto
{
    public int SessionId { get; set; }
    public int FinalPage { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public bool IsCompleted { get; set; }
}

public class ReadingHistoryDto
{
    public List<ReadingSessionDto> Sessions { get; set; } = new();
    public int TotalSessions { get; set; }
    public int TotalBooksRead { get; set; }
    public int TotalPagesRead { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public decimal TotalAmountSpent { get; set; }
    public Dictionary<string, int> ReadingByCategory { get; set; } = new();
}

public class StartReadingSessionDtoValidator : AbstractValidator<StartReadingSessionDto>
{
    public StartReadingSessionDtoValidator()
    {
        RuleFor(x => x.BookId)
            .GreaterThan(0).WithMessage("Valid book ID is required");

        RuleFor(x => x.PaymentType)
            .NotEmpty().WithMessage("Payment type is required")
            .Must(BeValidPaymentType).WithMessage("Payment type must be 'PerPage' or 'PerHour'");
    }

    private bool BeValidPaymentType(string paymentType)
    {
        return paymentType == "PerPage" || paymentType == "PerHour";
    }
}

public class UpdateReadingProgressDtoValidator : AbstractValidator<UpdateReadingProgressDto>
{
    public UpdateReadingProgressDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Valid session ID is required");

        RuleFor(x => x.CurrentPage)
            .GreaterThan(0).WithMessage("Current page must be greater than 0");

        RuleFor(x => x.TimeSpentMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Time spent cannot be negative");
    }
}

public class EndReadingSessionDtoValidator : AbstractValidator<EndReadingSessionDto>
{
    public EndReadingSessionDtoValidator()
    {
        RuleFor(x => x.SessionId)
            .GreaterThan(0).WithMessage("Valid session ID is required");

        RuleFor(x => x.FinalPage)
            .GreaterThan(0).WithMessage("Final page must be greater than 0");

        RuleFor(x => x.TotalTimeSpentMinutes)
            .GreaterThan(0).WithMessage("Total time spent must be greater than 0");
    }
}