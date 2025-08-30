using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ByteBook.Application.Interfaces;
using ByteBook.Domain.Repositories;
using ByteBook.Infrastructure.Caching;
using ByteBook.Infrastructure.Configurations;
using ByteBook.Infrastructure.Events;
using ByteBook.Infrastructure.Persistence;
using ByteBook.Infrastructure.Repositories;
using ByteBook.Infrastructure.Search;
using Elastic.Clients.Elasticsearch;
using StackExchange.Redis;

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

        // Redis Cache
        AddRedisCache(services, configuration);

        // Elasticsearch
        AddElasticsearch(services, configuration);

        return services;
    }

    private static void AddRedisCache(IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection(RedisConfiguration.SectionName).Get<RedisConfiguration>() 
                         ?? new RedisConfiguration();

        // Configure Redis connection
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = redisConfig.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            }

            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = redisConfig.AbortOnConnectFail;
            configurationOptions.ConnectRetry = redisConfig.ConnectRetry;
            configurationOptions.ConnectTimeout = (int)redisConfig.ConnectTimeout.TotalMilliseconds;
            configurationOptions.SyncTimeout = (int)redisConfig.SyncTimeout.TotalMilliseconds;

            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Configure distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            var connectionString = redisConfig.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            }
            
            options.Configuration = connectionString;
            options.InstanceName = redisConfig.InstanceName;
        });

        // Register cache services
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        services.AddSingleton<ISessionCacheService, RedisSessionCacheService>();
        
        // Register caching strategies
        services.AddScoped<ICachingStrategy, CachingStrategy>();
        services.AddScoped<BookCachingService>();
        services.AddScoped<UserCachingService>();
        services.AddScoped<SearchCachingService>();

        // Register configurations
        services.Configure<RedisConfiguration>(configuration.GetSection(RedisConfiguration.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));
    }

    private static void AddElasticsearch(IServiceCollection services, IConfiguration configuration)
    {
        var elasticsearchConfig = configuration.GetSection(ElasticsearchConfiguration.SectionName).Get<ElasticsearchConfiguration>() 
                                 ?? new ElasticsearchConfiguration();

        // Configure Elasticsearch client
        services.AddSingleton<ElasticsearchClient>(provider =>
        {
            var uri = new Uri(elasticsearchConfig.Uri);
            var settings = new ElasticsearchClientSettings(uri);

            if (!string.IsNullOrEmpty(elasticsearchConfig.Username) && !string.IsNullOrEmpty(elasticsearchConfig.Password))
            {
                settings.Authentication(new BasicAuthentication(elasticsearchConfig.Username, elasticsearchConfig.Password));
            }

            settings.RequestTimeout(elasticsearchConfig.RequestTimeout);
            settings.MaximumRetries(elasticsearchConfig.MaxRetries);
            settings.ThrowExceptions(elasticsearchConfig.ThrowExceptions);

            if (elasticsearchConfig.EnableDebugMode)
            {
                settings.EnableDebugMode();
            }

            return new ElasticsearchClient(settings);
        });

        // Register search services
        services.AddScoped<ISearchService, ElasticsearchService>();
        services.AddScoped<ISearchIndexService, SearchIndexService>();

        // Register configuration
        services.Configure<ElasticsearchConfiguration>(configuration.GetSection(ElasticsearchConfiguration.SectionName));
    }
}