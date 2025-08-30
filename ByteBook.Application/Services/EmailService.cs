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
            var verificationUrl = $"{_baseUrl}/auth/verify-email?token={token}&email={Uri.EscapeDataString(email)}";
            
            var subject = "Verify Your Email Address - ByteBook";
            var body = $@"
                <html>
                <body>
                    <h2>Welcome to ByteBook!</h2>
                    <p>Thank you for registering with ByteBook. Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email Address</a></p>
                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p>{verificationUrl}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't create an account with ByteBook, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>The ByteBook Team</p>
                </body>
                </html>";

            // In a real implementation, you would use an email service like SendGrid, AWS SES, etc.
            // For now, we'll just log the email content
            _logger.LogInformation("Email verification sent to {Email}. Verification URL: {Url}", email, verificationUrl);
            
            // Simulate email sending
            await Task.Delay(100, cancellationToken);
            
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
            var resetUrl = $"{_baseUrl}/auth/reset-password?token={token}&email={Uri.EscapeDataString(email)}";
            
            var subject = "Reset Your Password - ByteBook";
            var body = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password for your ByteBook account.</p>
                    <p>Click the link below to reset your password:</p>
                    <p><a href='{resetUrl}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                    <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                    <p>{resetUrl}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
                    <br>
                    <p>Best regards,<br>The ByteBook Team</p>
                </body>
                </html>";

            // In a real implementation, you would use an email service like SendGrid, AWS SES, etc.
            // For now, we'll just log the email content
            _logger.LogInformation("Password reset email sent to {Email}. Reset URL: {Url}", email, resetUrl);
            
            // Simulate email sending
            await Task.Delay(100, cancellationToken);
            
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
            var subject = "Welcome to ByteBook!";
            var body = $@"
                <html>
                <body>
                    <h2>Welcome to ByteBook, {firstName}!</h2>
                    <p>Your email has been successfully verified and your account is now active.</p>
                    <p>You can now:</p>
                    <ul>
                        <li>Browse and purchase books</li>
                        <li>Start reading your purchased books</li>
                        <li>Track your reading progress</li>
                        <li>Discover new authors and genres</li>
                    </ul>
                    <p><a href='{_baseUrl}/dashboard' style='background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Go to Dashboard</a></p>
                    <p>If you have any questions, feel free to contact our support team.</p>
                    <br>
                    <p>Happy reading!<br>The ByteBook Team</p>
                </body>
                </html>";

            // In a real implementation, you would use an email service like SendGrid, AWS SES, etc.
            // For now, we'll just log the email content
            _logger.LogInformation("Welcome email sent to {Email} for user {FirstName}", email, firstName);
            
            // Simulate email sending
            await Task.Delay(100, cancellationToken);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
            return Result.Failure("Failed to send welcome email");
        }
    }
}