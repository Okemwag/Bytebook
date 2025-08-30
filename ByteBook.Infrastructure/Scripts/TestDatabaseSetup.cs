using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ByteBook.Infrastructure;
using ByteBook.Infrastructure.Extensions;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Scripts;

public class TestDatabaseSetup
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            Console.WriteLine("Testing database setup...");
            
            // Test database connection
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            Console.WriteLine("Checking database connection...");
            var canConnect = await context.Database.CanConnectAsync();
            Console.WriteLine($"Database connection: {(canConnect ? "SUCCESS" : "FAILED")}");
            
            if (canConnect)
            {
                Console.WriteLine("Running migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("Migrations completed successfully");
                
                Console.WriteLine("Seeding database...");
                await host.SeedDatabaseAsync();
                Console.WriteLine("Database seeding completed successfully");
                
                // Test data retrieval
                var userCount = await context.Users.CountAsync();
                var bookCount = await context.Books.CountAsync();
                
                Console.WriteLine($"Database contains {userCount} users and {bookCount} books");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Database setup test completed");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
                services.AddLogging(builder => builder.AddConsole());
            });
}