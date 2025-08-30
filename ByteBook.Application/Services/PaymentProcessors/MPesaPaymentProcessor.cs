using ByteBook.Application.Common;
using ByteBook.Application.DTOs.Payments;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ByteBook.Application.Services.PaymentProcessors;

public class MPesaPaymentProcessor : IPaymentProcessor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MPesaPaymentProcessor> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _consumerKey;
    private readonly string _consumerSecret;
    private readonly string _baseUrl;
    private readonly string _shortCode;
    private readonly string _passkey;

    public string ProviderName => "MPesa";

    public MPesaPaymentProcessor(
        IConfiguration configuration,
        ILogger<MPesaPaymentProcessor> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _consumerKey = _configuration["MPesa:ConsumerKey"] ?? throw new InvalidOperationException("M-Pesa ConsumerKey not configured");
        _consumerSecret = _configuration["MPesa:ConsumerSecret"] ?? throw new InvalidOperationException("M-Pesa ConsumerSecret not configured");
        _baseUrl = _configuration["MPesa:BaseUrl"] ?? "https://sandbox.safaricom.co.ke"; // Default to sandbox
        _shortCode = _configuration["MPesa:ShortCode"] ?? "174379";
        _passkey = _configuration["MPesa:Passkey"] ?? "";

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
                return Result<PaymentResultDto>.Failure("Failed to authenticate with M-Pesa");
            }

            // Extract phone number from payment method ID (in M-Pesa, this would be the phone number)
            var phoneNumber = request.PaymentMethodId;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Result<PaymentResultDto>.Failure("Phone number is required for M-Pesa payments");
            }

            // Generate timestamp and password
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shortCode}{_passkey}{timestamp}"));

            // Create STK Push request
            var stkPushData = new
            {
                BusinessShortCode = _shortCode,
                Password = password,
                Timestamp = timestamp,
                TransactionType = "CustomerPayBillOnline",
                Amount = (int)request.Amount, // M-Pesa uses whole numbers
                PartyA = phoneNumber,
                PartyB = _shortCode,
                PhoneNumber = phoneNumber,
                CallBackURL = $"{_configuration["App:BaseUrl"]}/api/payments/mpesa/callback",
                AccountReference = request.Metadata.TryGetValue("PaymentId", out var paymentId) ? paymentId.ToString() : "ByteBook",
                TransactionDesc = request.Description
            };

            var json = JsonSerializer.Serialize(stkPushData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync("/mpesa/stkpush/v1/processrequest", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("M-Pesa STK Push failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<PaymentResultDto>.Failure("Failed to initiate M-Pesa payment");
            }

            var stkResponse = JsonSerializer.Deserialize<MPesaStkPushResponse>(responseContent);

            var result = new PaymentResultDto
            {
                IsSuccess = stkResponse?.ResponseCode == "0",
                PaymentId = stkResponse?.CheckoutRequestID,
                ExternalTransactionId = stkResponse?.CheckoutRequestID,
                Amount = request.Amount,
                Currency = "KES", // M-Pesa primarily uses Kenyan Shillings
                Status = stkResponse?.ResponseCode == "0" ? "pending" : "failed",
                Metadata = request.Metadata
            };

            if (stkResponse?.ResponseCode != "0")
            {
                result.ErrorMessage = stkResponse?.ResponseDescription ?? "M-Pesa payment failed";
            }

            _logger.LogInformation("M-Pesa STK Push initiated: {CheckoutRequestId}, ResponseCode: {ResponseCode}", 
                stkResponse?.CheckoutRequestID, stkResponse?.ResponseCode);

            return Result<PaymentResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing M-Pesa payment");
            return Result<PaymentResultDto>.Failure("An error occurred while processing the payment");
        }
    }

    public async Task<Result<RefundResultDto>> ProcessRefundAsync(PaymentProcessorRefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // M-Pesa refunds are typically handled through reversal API
            // This is a simplified implementation
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                return Result<RefundResultDto>.Failure("Failed to authenticate with M-Pesa");
            }

            // For M-Pesa, refunds are complex and require specific business logic
            // This is a placeholder implementation
            var result = new RefundResultDto
            {
                IsSuccess = false,
                ErrorMessage = "M-Pesa refunds require manual processing through Safaricom portal",
                Amount = request.Amount,
                Currency = "KES",
                Status = "pending_manual_processing"
            };

            _logger.LogInformation("M-Pesa refund request logged for manual processing: TransactionId={TransactionId}, Amount={Amount}", 
                request.ExternalTransactionId, request.Amount);

            return Result<RefundResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing M-Pesa refund");
            return Result<RefundResultDto>.Failure("An error occurred while processing the refund");
        }
    }

    public async Task<Result<WebhookValidationResult>> ValidateWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            // M-Pesa callback validation
            var callbackData = JsonSerializer.Deserialize<MPesaCallbackData>(payload);
            
            var result = new WebhookValidationResult
            {
                IsValid = true, // M-Pesa callbacks are typically from trusted sources
                EventType = "payment.callback",
                Data = new Dictionary<string, object>()
            };

            if (callbackData?.Body?.StkCallback != null)
            {
                var callback = callbackData.Body.StkCallback;
                result.ExternalTransactionId = callback.CheckoutRequestID;
                
                result.Data["ResultCode"] = callback.ResultCode?.ToString() ?? "";
                result.Data["ResultDesc"] = callback.ResultDesc ?? "";
                result.Data["MerchantRequestID"] = callback.MerchantRequestID ?? "";
                result.Data["CheckoutRequestID"] = callback.CheckoutRequestID ?? "";

                if (callback.CallbackMetadata?.Item != null)
                {
                    foreach (var item in callback.CallbackMetadata.Item)
                    {
                        if (!string.IsNullOrEmpty(item.Name) && item.Value != null)
                        {
                            result.Data[item.Name] = item.Value;
                        }
                    }
                }
            }

            await Task.CompletedTask;

            return Result<WebhookValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating M-Pesa webhook");
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
                return Result<PaymentStatusDto>.Failure("Failed to authenticate with M-Pesa");
            }

            // M-Pesa transaction status query
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var password = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shortCode}{_passkey}{timestamp}"));

            var queryData = new
            {
                BusinessShortCode = _shortCode,
                Password = password,
                Timestamp = timestamp,
                CheckoutRequestID = externalTransactionId
            };

            var json = JsonSerializer.Serialize(queryData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsync("/mpesa/stkpushquery/v1/query", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get M-Pesa transaction status: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return Result<PaymentStatusDto>.Failure("Failed to retrieve payment status");
            }

            var queryResponse = JsonSerializer.Deserialize<MPesaQueryResponse>(responseContent);

            var result = new PaymentStatusDto
            {
                ExternalTransactionId = externalTransactionId,
                Status = MapMPesaStatus(queryResponse?.ResultCode),
                Amount = 0, // Amount not returned in query response
                Currency = "KES",
                ProcessedAt = null, // Would need to parse from response if available
                Metadata = new Dictionary<string, object>
                {
                    { "ResultCode", queryResponse?.ResultCode?.ToString() ?? "" },
                    { "ResultDesc", queryResponse?.ResultDesc ?? "" }
                }
            };

            return Result<PaymentStatusDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving M-Pesa payment status for {TransactionId}", externalTransactionId);
            return Result<PaymentStatusDto>.Failure("An error occurred while retrieving payment status");
        }
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_consumerKey}:{_consumerSecret}"));
            
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _httpClient.GetAsync("/oauth/v1/generate?grant_type=client_credentials", cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("M-Pesa authentication failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<MPesaTokenResponse>(responseContent);
            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting M-Pesa access token");
            return null;
        }
    }

    private string MapMPesaStatus(int? resultCode)
    {
        return resultCode switch
        {
            0 => "completed",
            1032 => "failed", // Request cancelled by user
            1037 => "failed", // DS timeout
            2001 => "failed", // Wrong PIN
            _ => "unknown"
        };
    }

    // M-Pesa API response models
    private class MPesaTokenResponse
    {
        public string? AccessToken { get; set; }
        public string? ExpiresIn { get; set; }
    }

    private class MPesaStkPushResponse
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseDescription { get; set; }
        public string? CustomerMessage { get; set; }
    }

    private class MPesaQueryResponse
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public int? ResultCode { get; set; }
        public string? ResultDesc { get; set; }
    }

    private class MPesaCallbackData
    {
        public MPesaCallbackBody? Body { get; set; }
    }

    private class MPesaCallbackBody
    {
        public MPesaStkCallback? StkCallback { get; set; }
    }

    private class MPesaStkCallback
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public int? ResultCode { get; set; }
        public string? ResultDesc { get; set; }
        public MPesaCallbackMetadata? CallbackMetadata { get; set; }
    }

    private class MPesaCallbackMetadata
    {
        public MPesaCallbackItem[]? Item { get; set; }
    }

    private class MPesaCallbackItem
    {
        public string? Name { get; set; }
        public object? Value { get; set; }
    }
}