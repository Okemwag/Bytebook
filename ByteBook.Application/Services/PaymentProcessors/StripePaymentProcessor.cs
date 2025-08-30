using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ByteBook.Application.Services.PaymentProcessors;

public class StripePaymentProcessor : IPaymentProcessor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentProcessor> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly string _webhookSecret;

    public string ProviderName => "Stripe";

    public StripePaymentProcessor(
        IConfiguration configuration,
        ILogger<StripePaymentProcessor> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _secretKey = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not configured");
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe WebhookSecret not configured");
        
        _httpClient.BaseAddress = new Uri("https://api.stripe.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _secretKey);
    }

    public async Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentProcessorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create PaymentIntent with Stripe
            var paymentIntentData = new
            {
                amount = (int)(request.Amount * 100), // Stripe uses cents
                currency = request.Currency.ToLower(),
                payment_method = request.PaymentMethodId,
                confirmation_method = "manual",
                confirm = true,
                description = request.Description,
                metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? "")
            };

            var json = JsonSerializer.Serialize(paymentIntentData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/payment_intents", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe payment failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
                return Result<PaymentResultDto>.Failure(errorResponse?.Error?.Message ?? "Payment processing failed");
            }

            var paymentIntent = JsonSerializer.Deserialize<StripePaymentIntent>(responseContent);
            
            var result = new PaymentResultDto
            {
                IsSuccess = paymentIntent?.Status == "succeeded",
                PaymentId = paymentIntent?.Id,
                ExternalTransactionId = paymentIntent?.Id,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = MapStripeStatus(paymentIntent?.Status),
                Metadata = request.Metadata
            };

            // Handle cases where additional authentication is required
            if (paymentIntent?.Status == "requires_action" && paymentIntent.NextAction?.Type == "use_stripe_sdk")
            {
                result.PaymentUrl = paymentIntent.NextAction.UseStripeSDK?.Stripe3DS2Source;
                result.Status = "requires_action";
            }

            _logger.LogInformation("Stripe payment processed: {PaymentIntentId}, Status: {Status}", 
                paymentIntent?.Id, paymentIntent?.Status);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe payment");
            return Result<PaymentResultDto>.Failure("An error occurred while processing the payment");
        }
    }

    public async Task<Result<RefundResultDto>> ProcessRefundAsync(PaymentProcessorRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var refundData = new
            {
                payment_intent = request.ExternalTransactionId,
                amount = (int)(request.Amount * 100), // Stripe uses cents
                reason = "requested_by_customer",
                metadata = request.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString() ?? "")
            };

            var json = JsonSerializer.Serialize(refundData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/refunds", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stripe refund failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                
                var errorResponse = JsonSerializer.Deserialize<StripeErrorResponse>(responseContent);
                return Result<RefundResultDto>.Failure(errorResponse?.Error?.Message ?? "Refund processing failed");
            }

            var refund = JsonSerializer.Deserialize<StripeRefund>(responseContent);
            
            var result = new RefundResultDto
            {
                IsSuccess = refund?.Status == "succeeded",
                RefundId = refund?.Id,
                ExternalRefundId = refund?.Id,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = refund?.Status ?? "unknown",
                ProcessedAt = refund?.Created != null ? DateTimeOffset.FromUnixTimeSeconds(refund.Created.Value).DateTime : null
            };

            _logger.LogInformation("Stripe refund processed: {RefundId}, Status: {Status}", 
                refund?.Id, refund?.Status);

            return Result<RefundResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe refund");
            return Result<RefundResultDto>.Failure("An error occurred while processing the refund");
        }
    }

    public async Task<Result<WebhookValidationResult>> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate Stripe webhook signature
            if (!ValidateStripeSignature(payload, signature, _webhookSecret))
            {
                return Result<WebhookValidationResult>.Failure("Invalid webhook signature");
            }

            var webhookEvent = JsonSerializer.Deserialize<StripeWebhookEvent>(payload);
            
            var result = new WebhookValidationResult
            {
                IsValid = true,
                EventType = webhookEvent?.Type,
                Data = new Dictionary<string, object>()
            };

            // Extract relevant data based on event type
            if (webhookEvent?.Data?.Object != null)
            {
                var eventData = webhookEvent.Data.Object as JsonElement?;
                if (eventData.HasValue)
                {
                    if (eventData.Value.TryGetProperty("id", out var idElement))
                    {
                        result.ExternalTransactionId = idElement.GetString();
                    }

                    // Add other relevant properties to the data dictionary
                    foreach (var property in eventData.Value.EnumerateObject())
                    {
                        result.Data[property.Name] = property.Value.ToString();
                    }
                }
            }

            await Task.CompletedTask; // Satisfy async requirement

            return Result<WebhookValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Stripe webhook");
            return Result<WebhookValidationResult>.Failure("Error validating webhook");
        }
    }

    public async Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(string externalTransactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"v1/payment_intents/{externalTransactionId}", cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Stripe payment status: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<PaymentStatusDto>.Failure("Failed to retrieve payment status");
            }

            var paymentIntent = JsonSerializer.Deserialize<StripePaymentIntent>(responseContent);
            
            var result = new PaymentStatusDto
            {
                ExternalTransactionId = paymentIntent?.Id ?? externalTransactionId,
                Status = MapStripeStatus(paymentIntent?.Status),
                Amount = (paymentIntent?.Amount ?? 0) / 100m, // Convert from cents
                Currency = paymentIntent?.Currency?.ToUpper() ?? "USD",
                ProcessedAt = paymentIntent?.Created != null ? DateTimeOffset.FromUnixTimeSeconds(paymentIntent.Created.Value).DateTime : null,
                Metadata = paymentIntent?.Metadata?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>()
            };

            return Result<PaymentStatusDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Stripe payment status for {TransactionId}", externalTransactionId);
            return Result<PaymentStatusDto>.Failure("An error occurred while retrieving payment status");
        }
    }

    private bool ValidateStripeSignature(string payload, string signature, string secret)
    {
        try
        {
            // Stripe signature validation logic
            // This is a simplified version - in production, use Stripe's official validation
            var elements = signature.Split(',');
            var timestamp = elements.FirstOrDefault(e => e.StartsWith("t="))?.Substring(2);
            var signatures = elements.Where(e => e.StartsWith("v1=")).Select(e => e.Substring(3)).ToArray();

            if (string.IsNullOrEmpty(timestamp) || !signatures.Any())
                return false;

            var signedPayload = $"{timestamp}.{payload}";
            var expectedSignature = ComputeHmacSha256(signedPayload, secret);

            return signatures.Any(sig => sig.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    private string MapStripeStatus(string? stripeStatus)
    {
        return stripeStatus switch
        {
            "succeeded" => "completed",
            "processing" => "processing",
            "requires_payment_method" => "failed",
            "requires_confirmation" => "pending",
            "requires_action" => "requires_action",
            "canceled" => "failed",
            _ => "unknown"
        };
    }

    // Stripe API response models
    private class StripePaymentIntent
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public long? Amount { get; set; }
        public string? Currency { get; set; }
        public long? Created { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public StripeNextAction? NextAction { get; set; }
    }

    private class StripeNextAction
    {
        public string? Type { get; set; }
        public StripeUseStripeSDK? UseStripeSDK { get; set; }
    }

    private class StripeUseStripeSDK
    {
        public string? Stripe3DS2Source { get; set; }
    }

    private class StripeRefund
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public long? Amount { get; set; }
        public string? Currency { get; set; }
        public long? Created { get; set; }
    }

    private class StripeWebhookEvent
    {
        public string? Type { get; set; }
        public StripeEventData? Data { get; set; }
    }

    private class StripeEventData
    {
        public object? Object { get; set; }
    }

    private class StripeErrorResponse
    {
        public StripeError? Error { get; set; }
    }

    private class StripeError
    {
        public string? Message { get; set; }
        public string? Type { get; set; }
        public string? Code { get; set; }
    }
}