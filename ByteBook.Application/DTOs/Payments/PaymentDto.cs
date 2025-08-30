namespace ByteBook.Application.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public PaymentDetailsDto? Details { get; set; }
}

public class PaymentDetailsDto
{
    public int PagesRead { get; set; }
    public int TimeSpentMinutes { get; set; }
    public decimal RateUsed { get; set; }
    public string RateType { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class PaymentSummaryDto
{
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int TotalTransactions { get; set; }
    public Dictionary<string, decimal> AmountByProvider { get; set; } = new();
    public Dictionary<string, decimal> AmountByType { get; set; } = new();
    public Dictionary<string, int> TransactionsByStatus { get; set; } = new();
}

public class AuthorEarningsDto
{
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public decimal TotalEarnings { get; set; }
    public decimal PendingEarnings { get; set; }
    public decimal WithdrawnEarnings { get; set; }
    public string Currency { get; set; } = "USD";
    public List<BookEarningsDto> BookEarnings { get; set; } = new();
    public Dictionary<string, decimal> EarningsByDate { get; set; } = new();
}

public class BookEarningsDto
{
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public decimal TotalEarnings { get; set; }
    public int TotalReads { get; set; }
    public decimal AverageEarningPerRead { get; set; }
    public Dictionary<string, decimal> EarningsByPaymentType { get; set; } = new();
}