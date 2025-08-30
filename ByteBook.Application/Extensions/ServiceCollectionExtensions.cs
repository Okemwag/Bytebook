using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Services;
using ByteBook.Application.DTOs.Auth;

namespace ByteBook.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add FluentValidation
        services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
        
        // Add Application Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IPaymentService, PaymentService>();
        
        // Add AutoMapper if needed
        // services.AddAutoMapper(typeof(ServiceCollectionExtensions));
        
        return services;
    }
}