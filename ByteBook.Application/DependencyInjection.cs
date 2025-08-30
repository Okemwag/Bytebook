using System.Reflection;
using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ByteBook.Application.Behaviors;
using ByteBook.Application.Interfaces;
using ByteBook.Application.Mappings;
using ByteBook.Application.Services;

namespace ByteBook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // Add MediatR
        services.AddMediatR(Assembly.GetExecutingAssembly());

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add MediatR behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Add Application Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}