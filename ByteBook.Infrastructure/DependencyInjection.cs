using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ByteBook.Domain.Repositories;
using ByteBook.Infrastructure.Events;
using ByteBook.Infrastructure.Persistence;
using ByteBook.Infrastructure.Repositories;

namespace ByteBook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IReferralRepository, ReferralRepository>();

        // Domain Events
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Database Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}