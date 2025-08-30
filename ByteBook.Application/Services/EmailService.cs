using ByteBook.Application.Common;
using ByteBook.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByteBook.Application.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _baseUrl;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        _fromEmail = _configuration["Email:FromEmail"] ?? "noreply@bytebook.com";
        _fromName = _configuration["Email:FromName"] ?? "ByteBook Platform";
    }

    public async Task<Result> SendEmailVerificationAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var verificationUrl = $"{_baseUrl}/verify-email?token={token}&email={Uri.EscapeDataString(email)}";
            
            var subject = "Verify Your Email Address - ByteBook";
            var htmlBody = GenerateEmailVerificationHtml(verificationUrl);
            var textBody = GenerateEmailVerificationText(verificationUrl);

            // In a real implementation, you would use an email service like SendGrid, AWS SES, etc.
            // For now, we'll just log the email content
            _logger.LogInformation("Email verification sent to {Email}. Verification URL: {Url}", email, verificationUrl);
            
            // TODO: Implement actual email sending when email service is configured
            await SimulateEmailSending(email, subject, htmlBody, textBody, cancellationToken);
            
            return Result.Success();
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
            
            var subject = "Reset Your Password - ByteBook";
            var htmlBody = GeneratePasswordResetHtml(resetUrl);
            var textBody = GeneratePasswordResetText(resetUrl);

            _logger.LogInformation("Password reset email sent to {Email}. Reset URL: {Url}", email, resetUrl);
            
            // TODO: Implement actual email sending when email service is configured
            await SimulateEmailSending(email, subject, htmlBody, textBody, cancellationToken);
            
            return Result.Success();
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
            var subject = $"Welcome to ByteBook, {firstName}!";
            var htmlBody = GenerateWelcomeEmailHtml(firstName);
            var textBody = GenerateWelcomeEmailText(firstName);

            _logger.LogInformation("Welcome email sent to {Email}", email);
            
            // TODO: Implement actual email sending when email service is configured
            await SimulateEmailSending(email, subject, htmlBody, textBody, cancellationToken);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return Result.Failure("Failed to send welcome email");
        }
    }

    private async Task SimulateEmailSending(string email, string subject, string htmlBody, string textBody, CancellationToken cancellationToken)
    {
        // Simulate email sending delay
        await Task.Delay(100, cancellationToken);
        
        _logger.LogInformation("EMAIL SENT - To: {Email}, Subject: {Subject}", email, subject);
        _logger.LogDebug("Email HTML Body: {HtmlBody}", htmlBody);
        _logger.LogDebug("Email Text Body: {TextBody}", textBody);
    }

    private string GenerateEmailVerificationHtml(string verificationUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Verify Your Email</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Verify Your Email Address</h2>
        <p>Thank you for registering with ByteBook! Please click the button below to verify your email address:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{verificationUrl}' 
               style='background-color: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Verify Email Address
            </a>
        </div>
        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #7f8c8d;'>{verificationUrl}</p>
        <p>This link will expire in 24 hours for security reasons.</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='font-size: 12px; color: #7f8c8d;'>
            If you didn't create an account with ByteBook, please ignore this email.
        </p>
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
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Reset Your Password</h2>
        <p>We received a request to reset your password for your ByteBook account. Click the button below to reset it:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{resetUrl}' 
               style='background-color: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Reset Password
            </a>
        </div>
        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
        <p style='word-break: break-all; color: #7f8c8d;'>{resetUrl}</p>
        <p>This link will expire in 24 hours for security reasons.</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='font-size: 12px; color: #7f8c8d;'>
            If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
        </p>
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
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>Welcome to ByteBook, {firstName}!</h2>
        <p>We're excited to have you join our community of readers and authors.</p>
        <p>Here's what you can do with your new account:</p>
        <ul>
            <li>Discover and read amazing books from talented authors</li>
            <li>Create and publish your own books</li>
            <li>Connect with other readers and authors</li>
            <li>Track your reading progress and build your library</li>
        </ul>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{_baseUrl}' 
               style='background-color: #27ae60; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                Start Exploring
            </a>
        </div>
        <p>If you have any questions, feel free to reach out to our support team.</p>
        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
        <p style='font-size: 12px; color: #7f8c8d;'>
            Happy reading!<br>
            The ByteBook Team
        </p>
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
}