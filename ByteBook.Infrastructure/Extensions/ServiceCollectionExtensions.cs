using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ByteBook.Infrastructure.Persistence;

namespace ByteBook.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static async Task<IHost> SeedDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }

        return host;
    }
}