using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;

namespace ByteBook.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentRequestDto request, int userId, CancellationToken cancellationToken = default);
    Task<Result<RefundResultDto>> ProcessRefundAsync(RefundRequestDto request, int userId, CancellationToken cancellationToken = default);
    Task<Result<decimal>> CalculateChargesAsync(int userId, int bookId, int? pagesRead = null, int? timeSpentMinutes = null, CancellationToken cancellationToken = default);
    Task<Result<PaymentDto>> GetPaymentByIdAsync(int paymentId, int userId, CancellationToken cancellationToken = default);
    Task<Result<List<PaymentDto>>> GetUserPaymentsAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result<PaymentSummaryDto>> GetPaymentSummaryAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Result<AuthorEarningsDto>> GetAuthorEarningsAsync(int authorId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Result> HandleWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default);
}

public interface IPaymentProcessor
{
    string ProviderName { get; }
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentProcessorRequest request, CancellationToken cancellationToken = default);
    Task<Result<RefundResultDto>> ProcessRefundAsync(PaymentProcessorRefundRequest request, CancellationToken cancellationToken = default);
    Task<Result<WebhookValidationResult>> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
    Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(string externalTransactionId, CancellationToken cancellationToken = default);
}

public class PaymentProcessorRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethodId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class PaymentProcessorRefundRequest
{
    public string ExternalTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class WebhookValidationResult
{
    public bool IsValid { get; set; }
    public string? EventType { get; set; }
    public string? ExternalTransactionId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class PaymentStatusDto
{
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime? ProcessedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}