using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;

namespace ByteBook.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(int userId, PaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<RefundResultDto>> ProcessRefundAsync(int paymentId, RefundRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<decimal>> CalculateChargesAsync(int userId, int bookId, string paymentType, int? pagesRead = null, int? timeSpentMinutes = null, CancellationToken cancellationToken = default);
    Task<Result<AuthorEarningsDto>> GetAuthorEarningsAsync(int authorId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Result<PaymentSummaryDto>> GetPaymentSummaryAsync(int userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Result<List<PaymentDto>>> GetPaymentHistoryAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Result> HandleWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default);
}

public interface IPaymentProcessor
{
    string ProviderName { get; }
    Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<RefundResultDto>> ProcessRefundAsync(string externalTransactionId, decimal amount, string reason, CancellationToken cancellationToken = default);
    Task<Result> HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default);
}

public interface IPaymentProcessorFactory
{
    IPaymentProcessor GetProcessor(string providerName);
    IEnumerable<string> GetSupportedProviders();
}