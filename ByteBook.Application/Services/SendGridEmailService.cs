using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ByteBook.Application.Services;

public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public SendGridEmailService(
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = _configuration["SendGrid:ApiKey"] ?? throw new InvalidOperationException("SendGrid API Key not configured");
        _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@bytebook.com";
        _fromName = _configuration["Email:FromName"] ?? "ByteBook Platform";
        _baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";

        _httpClient.BaseAddress = new Uri("https://api.sendgrid.com/");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<Result> SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var verificationUrl = $"{_baseUrl}/verify-email?token={token}&email={Uri.EscapeDataString(email)}";
            
            var emailData = new SendGridEmailRequest
            {
                From = new SendGridEmailAddress { Email = _fromEmail, Name = _fromName },
                Personalizations = new[]
                {
                    new SendGridPersonalization
                    {
                        To = new[] { new SendGridEmailAddress { Email = email } },
                        Subject = "Verify Your Email Address - ByteBook",
                        DynamicTemplateData = new Dictionary<string, object>
                        {
                            { "verification_url", verificationUrl },
                            { "user_email", email }
                        }
                    }
                },
                TemplateId = _configuration["SendGrid:Templates:EmailVerification"],
                Content = new[]
                {
                    new SendGridContent
                    {
                        Type = "text/html",
                        Value = GenerateEmailVerificationHtml(verificationUrl)
                    },
                    new SendGridContent
                    {
                        Type = "text/plain",
                        Value = GenerateEmailVerificationText(verificationUrl)
                    }
                }
            };

            var result = await SendEmailAsync(emailData, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Email verification sent successfully to {Email}", email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to {Email}", email);
            return Result.Failure("Failed to send verification email");
        }
    }

    public async Task<Result> SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var resetUrl = $"{_baseUrl}/reset-password?token={token}&email={Uri.EscapeDataString(email)}";
            
            var emailData = new SendGridEmailRequest
            {
                From = new SendGridEmailAddress { Email = _fromEmail, Name = _fromName },
                Personalizations = new[]
                {
                    new SendGridPersonalization
                    {
                        To = new[] { new SendGridEmailAddress { Email = email } },
                        Subject = "Reset Your Password - ByteBook",
                        DynamicTemplateData = new Dictionary<string, object>
                        {
                            { "reset_url", resetUrl },
                            { "user_email", email }
                        }
                    }
                },
                TemplateId = _configuration["SendGrid:Templates:PasswordReset"],
                Content = new[]
                {
                    new SendGridContent
                    {
                        Type = "text/html",
                        Value = GeneratePasswordResetHtml(resetUrl)
                    },
                    new SendGridContent
                    {
                        Type = "text/plain",
                        Value = GeneratePasswordResetText(resetUrl)
                    }
                }
            };

            var result = await SendEmailAsync(emailData, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Password reset email sent successfully to {Email}", email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return Result.Failure("Failed to send password reset email");
        }
    }

    public async Task<Result> SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailData = new SendGridEmailRequest
            {
                From = new SendGridEmailAddress { Email = _fromEmail, Name = _fromName },
                Personalizations = new[]
                {
                    new SendGridPersonalization
                    {
                        To = new[] { new SendGridEmailAddress { Email = email } },
                        Subject = $"Welcome to ByteBook, {firstName}!",
                        DynamicTemplateData = new Dictionary<string, object>
                        {
                            { "first_name", firstName },
                            { "user_email", email },
                            { "platform_url", _baseUrl }
                        }
                    }
                },
                TemplateId = _configuration["SendGrid:Templates:Welcome"],
                Content = new[]
                {
                    new SendGridContent
                    {
                        Type = "text/html",
                        Value = GenerateWelcomeEmailHtml(firstName)
                    },
                    new SendGridContent
                    {
                        Type = "text/plain",
                        Value = GenerateWelcomeEmailText(firstName)
                    }
                }
            };

            var result = await SendEmailAsync(emailData, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Welcome email sent successfully to {Email}", email);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return Result.Failure("Failed to send welcome email");
        }
    }

    private async Task<Result> SendEmailAsync(SendGridEmailRequest emailData, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(emailData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("v3/mail/send", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            
            return Result.Failure($"Email sending failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SendGrid");
            return Result.Failure("Email sending failed due to network error");
        }
    }

    private string GenerateEmailVerificationHtml(string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify Your Email</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background-color: #f9f9f9; }}
        .button {{ background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #7f8c8d; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ByteBook</h1>
        </div>
        <div class='content'>
            <h2>Verify Your Email Address</h2>
            <p>Thank you for registering with ByteBook! Please click the button below to verify your email address:</p>
            <div style='text-align: center;'>
                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
            </div>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #7f8c8d;'>{verificationUrl}</p>
            <p>This link will expire in 24 hours for security reasons.</p>
        </div>
        <div class='footer'>
            <p>If you didn't create an account with ByteBook, please ignore this email.</p>
            <p>&copy; 2024 ByteBook Platform. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateEmailVerificationText(string verificationUrl)
    {
        return $@"
Verify Your Email Address

Thank you for registering with ByteBook! Please visit the following link to verify your email address:

{verificationUrl}

This link will expire in 24 hours for security reasons.

If you didn't create an account with ByteBook, please ignore this email.

---
ByteBook Platform
";
    }

    private string GeneratePasswordResetHtml(string resetUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reset Your Password</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background-color: #f9f9f9; }}
        .button {{ background-color: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #7f8c8d; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ByteBook</h1>
        </div>
        <div class='content'>
            <h2>Reset Your Password</h2>
            <p>We received a request to reset your password for your ByteBook account. Click the button below to reset it:</p>
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </div>
            <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
            <p style='word-break: break-all; color: #7f8c8d;'>{resetUrl}</p>
            <p>This link will expire in 24 hours for security reasons.</p>
        </div>
        <div class='footer'>
            <p>If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
            <p>&copy; 2024 ByteBook Platform. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GeneratePasswordResetText(string resetUrl)
    {
        return $@"
Reset Your Password

We received a request to reset your password for your ByteBook account. Please visit the following link to reset it:

{resetUrl}

This link will expire in 24 hours for security reasons.

If you didn't request a password reset, please ignore this email. Your password will remain unchanged.

---
ByteBook Platform
";
    }

    private string GenerateWelcomeEmailHtml(string firstName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to ByteBook</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background-color: #f9f9f9; }}
        .button {{ background-color: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #7f8c8d; }}
        .feature {{ background-color: white; padding: 15px; margin: 10px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to ByteBook, {firstName}!</h1>
        </div>
        <div class='content'>
            <p>We're excited to have you join our community of readers and authors.</p>
            <h3>Here's what you can do with your new account:</h3>
            <div class='feature'>
                <h4>📚 Discover Amazing Books</h4>
                <p>Browse our collection of books and pay only for what you read</p>
            </div>
            <div class='feature'>
                <h4>✍️ Publish Your Own Content</h4>
                <p>Share your knowledge and earn from your writing</p>
            </div>
            <div class='feature'>
                <h4>🤝 Connect with Community</h4>
                <p>Engage with other readers and authors</p>
            </div>
            <div class='feature'>
                <h4>📊 Track Your Progress</h4>
                <p>Monitor your reading journey and earnings</p>
            </div>
            <div style='text-align: center;'>
                <a href='{_baseUrl}' class='button'>Start Exploring</a>
            </div>
        </div>
        <div class='footer'>
            <p>Happy reading!</p>
            <p>&copy; 2024 ByteBook Platform. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    private string GenerateWelcomeEmailText(string firstName)
    {
        return $@"
Welcome to ByteBook, {firstName}!

We're excited to have you join our community of readers and authors.

Here's what you can do with your new account:
- Discover and read amazing books from talented authors
- Create and publish your own books
- Connect with other readers and authors
- Track your reading progress and build your library

Visit {_baseUrl} to start exploring!

If you have any questions, feel free to reach out to our support team.

Happy reading!
The ByteBook Team
";
    }

    // SendGrid API models
    private class SendGridEmailRequest
    {
        public SendGridEmailAddress From { get; set; } = new();
        public SendGridPersonalization[] Personalizations { get; set; } = Array.Empty<SendGridPersonalization>();
        public string? TemplateId { get; set; }
        public SendGridContent[]? Content { get; set; }
    }

    private class SendGridEmailAddress
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private class SendGridPersonalization
    {
        public SendGridEmailAddress[] To { get; set; } = Array.Empty<SendGridEmailAddress>();
        public string Subject { get; set; } = string.Empty;
        public Dictionary<string, object>? DynamicTemplateData { get; set; }
    }

    private class SendGridContent
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}