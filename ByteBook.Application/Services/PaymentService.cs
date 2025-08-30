using AutoMapper;
using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Entities;
using ByteBook.Domain.Repositories;
using ByteBook.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IUserRepository _userRepository;
    private readonly IReadingRepository _readingRepository;
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly decimal _platformCommissionRate;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IBookRepository bookRepository,
        IUserRepository userRepository,
        IReadingRepository readingRepository,
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _bookRepository = bookRepository;
        _userRepository = userRepository;
        _readingRepository = readingRepository;
        _paymentProcessors = paymentProcessors;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
        _platformCommissionRate = decimal.Parse(_configuration["Payment:PlatformCommissionRate"] ?? "0.15");
    }

    public async Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Result<PaymentResultDto>.ValidationFailure("User", "User not found");
            }

            // Validate book exists and is accessible
            var book = await _bookRepository.GetByIdAsync(request.BookId);
            if (book == null)
            {
                return Result<PaymentResultDto>.ValidationFailure("Book", "Book not found");
            }

            if (!book.CanBeAccessedBy(userId))
            {
                return Result<PaymentResultDto>.ValidationFailure("Access", "You don't have permission to access this book");
            }

            // Calculate payment amount
            var chargeResult = await CalculateChargesAsync(userId, request.BookId, request.PagesRead, request.TimeSpentMinutes, cancellationToken);
            if (chargeResult.IsFailure)
            {
                return Result<PaymentResultDto>.Failure($"Charge calculation failed: {chargeResult.ErrorMessage}");
            }

            var amount = new Money(chargeResult.Value!);
            var paymentType = request.PaymentType == "PerPage" ? PaymentType.PerPage : PaymentType.PerHour;
            var provider = Enum.Parse<PaymentProvider>(request.Provider);

            // Create payment entity
            var payment = new Payment(userId, request.BookId, amount, paymentType, provider);
            var savedPayment = await _paymentRepository.AddAsync(payment);

            // Get payment processor
            var processor = _paymentProcessors.FirstOrDefault(p => p.ProviderName.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));
            if (processor == null)
            {
                payment.MarkAsFailed($"Payment processor '{request.Provider}' not available");
                await _paymentRepository.UpdateAsync(payment);
                return Result<PaymentResultDto>.Failure($"Payment processor '{request.Provider}' not available");
            }

            // Prepare processor request
            var processorRequest = new PaymentProcessorRequest
            {
                Amount = amount.Amount,
                Currency = amount.Currency,
                PaymentMethodId = request.PaymentMethodId ?? "",
                Description = $"Payment for {book.Title}",
                Metadata = new Dictionary<string, object>
                {
                    { "PaymentId", savedPayment.Id },
                    { "UserId", userId },
                    { "BookId", request.BookId },
                    { "PaymentType", request.PaymentType }
                }
            };

            if (request.Metadata != null)
            {
                foreach (var item in request.Metadata)
                {
                    processorRequest.Metadata.TryAdd(item.Key, item.Value);
                }
            }

            // Process payment through provider
            var processorResult = await processor.ProcessPaymentAsync(processorRequest, cancellationToken);
            if (processorResult.IsFailure)
            {
                payment.MarkAsFailed(processorResult.ErrorMessage ?? "Payment processing failed");
                await _paymentRepository.UpdateAsync(payment);
                return Result<PaymentResultDto>.Failure(processorResult.ErrorMessage ?? "Payment processing failed");
            }

            // Update payment with processor result
            if (!string.IsNullOrEmpty(processorResult.Value!.ExternalTransactionId))
            {
                payment.MarkAsProcessing(processorResult.Value.ExternalTransactionId);
            }

            if (processorResult.Value.Status == "completed")
            {
                payment.MarkAsCompleted();
            }

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Payment processed successfully: PaymentId={PaymentId}, Amount={Amount}, Provider={Provider}", 
                savedPayment.Id, amount.Amount, request.Provider);

            return Result<PaymentResultDto>.Success(processorResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for user {UserId}, book {BookId}", userId, request.BookId);
            return Result<PaymentResultDto>.Failure("An error occurred while processing the payment");
        }
    }

    public async Task<Result<RefundResultDto>> ProcessRefundAsync(RefundRequestDto request, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
            if (payment == null)
            {
                return Result<RefundResultDto>.ValidationFailure("Payment", "Payment not found");
            }

            // Verify user owns the payment or is the book author
            var book = await _bookRepository.GetByIdAsync(payment.BookId);
            if (payment.UserId != userId && book?.AuthorId != userId)
            {
                return Result<RefundResultDto>.ValidationFailure("Authorization", "You don't have permission to refund this payment");
            }

            if (!payment.IsRefundable())
            {
                return Result<RefundResultDto>.ValidationFailure("Refund", "Payment is not refundable");
            }

            var refundAmount = request.Amount.HasValue ? new Money(request.Amount.Value) : payment.GetRefundableAmount();
            
            if (refundAmount.IsZero)
            {
                return Result<RefundResultDto>.ValidationFailure("Amount", "No refundable amount available");
            }

            // Get payment processor
            var processor = _paymentProcessors.FirstOrDefault(p => p.ProviderName.Equals(payment.Provider.ToString(), StringComparison.OrdinalIgnoreCase));
            if (processor == null)
            {
                return Result<RefundResultDto>.Failure($"Payment processor '{payment.Provider}' not available");
            }

            // Process refund through provider
            var refundRequest = new PaymentProcessorRefundRequest
            {
                ExternalTransactionId = payment.ExternalTransactionId!,
                Amount = refundAmount.Amount,
                Currency = refundAmount.Currency,
                Reason = request.Reason,
                Metadata = new Dictionary<string, object>
                {
                    { "PaymentId", payment.Id },
                    { "RefundReason", request.Reason }
                }
            };

            var refundResult = await processor.ProcessRefundAsync(refundRequest, cancellationToken);
            if (refundResult.IsFailure)
            {
                return Result<RefundResultDto>.Failure(refundResult.ErrorMessage ?? "Refund processing failed");
            }

            // Update payment entity
            payment.ProcessRefund(refundAmount);
            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("Refund processed successfully: PaymentId={PaymentId}, Amount={Amount}", 
                payment.Id, refundAmount.Amount);

            return Result<RefundResultDto>.Success(refundResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", request.PaymentId);
            return Result<RefundResultDto>.Failure("An error occurred while processing the refund");
        }
    }

    public async Task<Result<decimal>> CalculateChargesAsync(int userId, int bookId, int? pagesRead = null, int? timeSpentMinutes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
            {
                return Result<decimal>.ValidationFailure("Book", "Book not found");
            }

            // If user is the author, no charge
            if (book.AuthorId == userId)
            {
                return Result<decimal>.Success(0m);
            }

            decimal charge = 0m;

            // Calculate per-page charge
            if (pagesRead.HasValue && book.PricePerPage != null)
            {
                var pageCharge = book.CalculatePageCharge(pagesRead.Value);
                charge += pageCharge.Amount;
            }

            // Calculate per-hour charge
            if (timeSpentMinutes.HasValue && book.PricePerHour != null)
            {
                var timeSpent = TimeSpan.FromMinutes(timeSpentMinutes.Value);
                var timeCharge = book.CalculateTimeCharge(timeSpent);
                charge += timeCharge.Amount;
            }

            return Result<decimal>.Success(charge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating charges for user {UserId}, book {BookId}", userId, bookId);
            return Result<decimal>.Failure("An error occurred while calculating charges");
        }
    }

    public async Task<Result<PaymentDto>> GetPaymentByIdAsync(int paymentId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return Result<PaymentDto>.ValidationFailure("Payment", "Payment not found");
            }

            // Verify user owns the payment or is the book author
            var book = await _bookRepository.GetByIdAsync(payment.BookId);
            if (payment.UserId != userId && book?.AuthorId != userId)
            {
                return Result<PaymentDto>.ValidationFailure("Authorization", "You don't have permission to view this payment");
            }

            var paymentDto = _mapper.Map<PaymentDto>(payment);
            if (book != null)
            {
                paymentDto.BookTitle = book.Title;
            }

            return Result<PaymentDto>.Success(paymentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment {PaymentId}", paymentId);
            return Result<PaymentDto>.Failure("An error occurred while retrieving the payment");
        }
    }

    public async Task<Result<List<PaymentDto>>> GetUserPaymentsAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId, page, pageSize);
            var paymentDtos = _mapper.Map<List<PaymentDto>>(payments);

            // Enrich with book titles
            var bookIds = paymentDtos.Select(p => p.BookId).Distinct().ToList();
            var books = await _bookRepository.GetByIdsAsync(bookIds);
            var bookTitleMap = books.ToDictionary(b => b.Id, b => b.Title);

            foreach (var paymentDto in paymentDtos)
            {
                if (bookTitleMap.TryGetValue(paymentDto.BookId, out var title))
                {
                    paymentDto.BookTitle = title;
                }
            }

            return Result<List<PaymentDto>>.Success(paymentDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for user {UserId}", userId);
            return Result<List<PaymentDto>>.Failure("An error occurred while retrieving payments");
        }
    }

    public async Task<Result<PaymentSummaryDto>> GetPaymentSummaryAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _paymentRepository.GetPaymentSummaryAsync(userId, fromDate, toDate);
            return Result<PaymentSummaryDto>.Success(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment summary for user {UserId}", userId);
            return Result<PaymentSummaryDto>.Failure("An error occurred while retrieving payment summary");
        }
    }

    public async Task<Result<AuthorEarningsDto>> GetAuthorEarningsAsync(int authorId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var earnings = await _paymentRepository.GetAuthorEarningsAsync(authorId, fromDate, toDate, _platformCommissionRate);
            return Result<AuthorEarningsDto>.Success(earnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving author earnings for author {AuthorId}", authorId);
            return Result<AuthorEarningsDto>.Failure("An error occurred while retrieving author earnings");
        }
    }

    public async Task<Result> HandleWebhookAsync(string provider, string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            var processor = _paymentProcessors.FirstOrDefault(p => p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
            if (processor == null)
            {
                _logger.LogWarning("Webhook received for unknown provider: {Provider}", provider);
                return Result.Failure($"Unknown payment provider: {provider}");
            }

            var validationResult = await processor.ValidateWebhookAsync(payload, signature, cancellationToken);
            if (validationResult.IsFailure || !validationResult.Value!.IsValid)
            {
                _logger.LogWarning("Invalid webhook signature for provider: {Provider}", provider);
                return Result.Failure("Invalid webhook signature");
            }

            var webhookData = validationResult.Value;
            
            // Handle different webhook events
            switch (webhookData.EventType?.ToLower())
            {
                case "payment.completed":
                case "payment_intent.succeeded":
                    await HandlePaymentCompletedWebhook(webhookData.ExternalTransactionId!, webhookData.Data);
                    break;
                
                case "payment.failed":
                case "payment_intent.payment_failed":
                    await HandlePaymentFailedWebhook(webhookData.ExternalTransactionId!, webhookData.Data);
                    break;
                
                case "refund.completed":
                case "charge.dispute.created":
                    await HandleRefundWebhook(webhookData.ExternalTransactionId!, webhookData.Data);
                    break;
                
                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType} for provider: {Provider}", 
                        webhookData.EventType, provider);
                    break;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook for provider {Provider}", provider);
            return Result.Failure("An error occurred while processing the webhook");
        }
    }

    private async Task HandlePaymentCompletedWebhook(string externalTransactionId, Dictionary<string, object> data)
    {
        var payment = await _paymentRepository.GetByExternalTransactionIdAsync(externalTransactionId);
        if (payment != null && payment.Status == PaymentStatus.Processing)
        {
            payment.MarkAsCompleted();
            await _paymentRepository.UpdateAsync(payment);
            
            _logger.LogInformation("Payment marked as completed via webhook: {ExternalTransactionId}", externalTransactionId);
        }
    }

    private async Task HandlePaymentFailedWebhook(string externalTransactionId, Dictionary<string, object> data)
    {
        var payment = await _paymentRepository.GetByExternalTransactionIdAsync(externalTransactionId);
        if (payment != null && payment.Status != PaymentStatus.Completed)
        {
            var failureReason = data.TryGetValue("failure_reason", out var reason) ? reason.ToString() : "Payment failed";
            payment.MarkAsFailed(failureReason!);
            await _paymentRepository.UpdateAsync(payment);
            
            _logger.LogInformation("Payment marked as failed via webhook: {ExternalTransactionId}, Reason: {Reason}", 
                externalTransactionId, failureReason);
        }
    }

    private async Task HandleRefundWebhook(string externalTransactionId, Dictionary<string, object> data)
    {
        var payment = await _paymentRepository.GetByExternalTransactionIdAsync(externalTransactionId);
        if (payment != null && payment.Status == PaymentStatus.Completed)
        {
            if (data.TryGetValue("refund_amount", out var amountObj) && decimal.TryParse(amountObj.ToString(), out var refundAmount))
            {
                var refundMoney = new Money(refundAmount, payment.Amount.Currency);
                payment.ProcessRefund(refundMoney);
                await _paymentRepository.UpdateAsync(payment);
                
                _logger.LogInformation("Refund processed via webhook: {ExternalTransactionId}, Amount: {Amount}", 
                    externalTransactionId, refundAmount);
            }
        }
    }
}