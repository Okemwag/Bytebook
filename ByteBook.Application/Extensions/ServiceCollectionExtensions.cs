using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ByteBook.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        var emailProvider = configuration["Email:Provider"]?.ToLower() ?? "local";

        switch (emailProvider)
        {
            case "sendgrid":
                services.AddHttpClient<SendGridEmailService>();
                services.AddScoped<IEmailService, SendGridEmailService>();
                break;
            
            case "local":
            default:
                services.AddScoped<IEmailService, EmailService>();
                break;
        }

        return services;
    }

    public static IServiceCollection AddFileStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        var storageProvider = configuration["FileStorage:Provider"]?.ToLower() ?? "local";

        switch (storageProvider)
        {
            case "aws":
            case "s3":
                services.AddHttpClient<AwsS3FileStorageService>();
                services.AddScoped<IFileStorageService, AwsS3FileStorageService>();
                break;
            
            case "azure":
            case "blob":
                services.AddHttpClient<AzureBlobStorageService>();
                services.AddScoped<IFileStorageService, AzureBlobStorageService>();
                break;
            
            case "local":
            default:
                services.AddScoped<IFileStorageService, FileStorageService>();
                break;
        }

        return services;
    }

    public static IServiceCollection AddContentProcessingServices(this IServiceCollection services)
    {
        services.AddScoped<IContentProcessingService, ContentProcessingService>();
        services.AddScoped<IContentProtectionService, ContentProtectionService>();
        return services;
    }

    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        
        // Register payment processors
        var enabledProviders = configuration.GetSection("Payment:EnabledProviders").Get<string[]>() ?? new[] { "Stripe" };

        foreach (var provider in enabledProviders)
        {
            switch (provider.ToLower())
            {
                case "stripe":
                    services.AddHttpClient<StripePaymentProcessor>();
                    services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
                    break;
                
                case "paypal":
                    services.AddHttpClient<PayPalPaymentProcessor>();
                    services.AddScoped<IPaymentProcessor, PayPalPaymentProcessor>();
                    break;
                
                case "mpesa":
                    services.AddHttpClient<MPesaPaymentProcessor>();
                    services.AddScoped<IPaymentProcessor, MPesaPaymentProcessor>();
                    break;
            }
        }

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        return services;
    }

    public static IServiceCollection AddBookServices(this IServiceCollection services)
    {
        services.AddScoped<IBookService, BookService>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEmailServices(configuration);
        services.AddFileStorageServices(configuration);
        services.AddContentProcessingServices();
        services.AddPaymentServices(configuration);
        services.AddAuthenticationServices();
        services.AddBookServices();

        return services;
    }
}