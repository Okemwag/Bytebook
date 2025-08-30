using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ByteBook.Application.Services.PaymentProcessors;

public class PayPalPaymentProcessor : IPaymentProcessor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalPaymentProcessor> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _baseUrl;
    private readonly string _webhookId;

    public string ProviderName => "PayPal";

    public PayPalPaymentProcessor(
        IConfiguration configuration,
        ILogger<PayPalPaymentProcessor> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _clientId = _configuration["PayPal:ClientId"] ?? throw new InvalidOperationException("PayPal ClientId not configured");
        _clientSecret = _configuration["PayPal:ClientSecret"] ?? throw new InvalidOperationException("PayPal ClientSecret not configured");
        _baseUrl = _configuration["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com"; // Default to sandbox
        _webhookId = _configuration["PayPal:WebhookId"] ?? "";

        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<Result<PaymentResultDto>> ProcessPaymentAsync(PaymentProcessorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get access token
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return Result<PaymentResultDto>.Failure("Failed to authenticate with PayPal");
            }

            // Create PayPal order
            var orderData = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = request.Currency.ToUpper(),
                            value = request.Amount.ToString("F2")
                        },
                        description = request.Description
                    }
                },
                application_context = new
                {
                    return_url = request.ReturnUrl ?? $"{_configuration["App:BaseUrl"]}/payment/success",
                    cancel_url = request.CancelUrl ?? $"{_configuration["App:BaseUrl"]}/payment/cancel",
                    brand_name = "ByteBook",
                    user_action = "PAY_NOW"
                }
            };

            var json = JsonSerializer.Serialize(orderData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Add("PayPal-Request-Id", Guid.NewGuid().ToString());

            var response = await _httpClient.PostAsync("/v2/checkout/orders", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal order creation failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<PaymentResultDto>.Failure("Failed to create PayPal order");
            }

            var order = JsonSerializer.Deserialize<PayPalOrder>(responseContent);
            var approvalUrl = order?.Links?.FirstOrDefault(l => l.Rel == "approve")?.Href;

            var result = new PaymentResultDto
            {
                IsSuccess = order?.Status == "CREATED",
                PaymentId = order?.Id,
                ExternalTransactionId = order?.Id,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = MapPayPalStatus(order?.Status),
                PaymentUrl = approvalUrl,
                Metadata = request.Metadata
            };

            _logger.LogInformation("PayPal order created: {OrderId}, Status: {Status}", order?.Id, order?.Status);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal payment");
            return Result<PaymentResultDto>.Failure("An error occurred while processing the payment");
        }
    }

    public async Task<Result<RefundResultDto>> ProcessRefundAsync(PaymentProcessorRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return Result<RefundResultDto>.Failure("Failed to authenticate with PayPal");
            }

            // First, get the capture ID from the order
            var captureId = await GetCaptureIdFromOrderAsync(request.ExternalTransactionId, accessToken, cancellationToken);
            if (string.IsNullOrEmpty(captureId))
            {
                return Result<RefundResultDto>.Failure("Failed to find capture for refund");
            }

            var refundData = new
            {
                amount = new
                {
                    currency_code = request.Currency.ToUpper(),
                    value = request.Amount.ToString("F2")
                },
                note_to_payer = request.Reason
            };

            var json = JsonSerializer.Serialize(refundData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync($"/v2/payments/captures/{captureId}/refund", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal refund failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<RefundResultDto>.Failure("Failed to process PayPal refund");
            }

            var refund = JsonSerializer.Deserialize<PayPalRefund>(responseContent);

            var result = new RefundResultDto
            {
                IsSuccess = refund?.Status == "COMPLETED",
                RefundId = refund?.Id,
                ExternalRefundId = refund?.Id,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = refund?.Status?.ToLower() ?? "unknown",
                ProcessedAt = DateTime.TryParse(refund?.CreateTime, out var createTime) ? createTime : null
            };

            _logger.LogInformation("PayPal refund processed: {RefundId}, Status: {Status}", refund?.Id, refund?.Status);

            return Result<RefundResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal refund");
            return Result<RefundResultDto>.Failure("An error occurred while processing the refund");
        }
    }

    public async Task<Result<WebhookValidationResult>> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            // PayPal webhook validation is more complex and requires calling their verification API
            // For now, we'll implement a basic validation
            var webhookEvent = JsonSerializer.Deserialize<PayPalWebhookEvent>(payload);
            
            var result = new WebhookValidationResult
            {
                IsValid = true, // In production, implement proper PayPal webhook verification
                EventType = webhookEvent?.EventType,
                Data = new Dictionary<string, object>()
            };

            if (webhookEvent?.Resource != null)
            {
                var resourceData = webhookEvent.Resource as JsonElement?;
                if (resourceData.HasValue)
                {
                    if (resourceData.Value.TryGetProperty("id", out var idElement))
                    {
                        result.ExternalTransactionId = idElement.GetString();
                    }

                    foreach (var property in resourceData.Value.EnumerateObject())
                    {
                        result.Data[property.Name] = property.Value.ToString();
                    }
                }
            }

            await Task.CompletedTask;

            return Result<WebhookValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PayPal webhook");
            return Result<WebhookValidationResult>.Failure("Error validating webhook");
        }
    }

    public async Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(string externalTransactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return Result<PaymentStatusDto>.Failure("Failed to authenticate with PayPal");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"/v2/checkout/orders/{externalTransactionId}", cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get PayPal order status: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<PaymentStatusDto>.Failure("Failed to retrieve payment status");
            }

            var order = JsonSerializer.Deserialize<PayPalOrder>(responseContent);
            var amount = decimal.TryParse(order?.PurchaseUnits?.FirstOrDefault()?.Amount?.Value, out var amt) ? amt : 0m;
            var currency = order?.PurchaseUnits?.FirstOrDefault()?.Amount?.CurrencyCode ?? "USD";

            var result = new PaymentStatusDto
            {
                ExternalTransactionId = order?.Id ?? externalTransactionId,
                Status = MapPayPalStatus(order?.Status),
                Amount = amount,
                Currency = currency,
                ProcessedAt = DateTime.TryParse(order?.CreateTime, out var createTime) ? createTime : null,
                Metadata = new Dictionary<string, object>()
            };

            return Result<PaymentStatusDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayPal payment status for {TransactionId}", externalTransactionId);
            return Result<PaymentStatusDto>.Failure("An error occurred while retrieving payment status");
        }
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
            
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.PostAsync("/v1/oauth2/token", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal authentication failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(responseContent);
            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal access token");
            return null;
        }
    }

    private async Task<string?> GetCaptureIdFromOrderAsync(string orderId, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"/v2/checkout/orders/{orderId}", cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var order = JsonSerializer.Deserialize<PayPalOrder>(responseContent);
            return order?.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault()?.Id;
        }
        catch
        {
            return null;
        }
    }

    private string MapPayPalStatus(string? paypalStatus)
    {
        return paypalStatus switch
        {
            "CREATED" => "pending",
            "SAVED" => "pending",
            "APPROVED" => "processing",
            "VOIDED" => "failed",
            "COMPLETED" => "completed",
            "PAYER_ACTION_REQUIRED" => "requires_action",
            _ => "unknown"
        };
    }

    // PayPal API response models
    private class PayPalTokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }

    private class PayPalOrder
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? CreateTime { get; set; }
        public PayPalLink[]? Links { get; set; }
        public PayPalPurchaseUnit[]? PurchaseUnits { get; set; }
    }

    private class PayPalLink
    {
        public string? Href { get; set; }
        public string? Rel { get; set; }
        public string? Method { get; set; }
    }

    private class PayPalPurchaseUnit
    {
        public PayPalAmount? Amount { get; set; }
        public PayPalPayments? Payments { get; set; }
    }

    private class PayPalAmount
    {
        public string? CurrencyCode { get; set; }
        public string? Value { get; set; }
    }

    private class PayPalPayments
    {
        public PayPalCapture[]? Captures { get; set; }
    }

    private class PayPalCapture
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
    }

    private class PayPalRefund
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? CreateTime { get; set; }
    }

    private class PayPalWebhookEvent
    {
        public string? EventType { get; set; }
        public object? Resource { get; set; }
    }
}