namespace ByteBook.Infrastructure.Configurations;

public class RedisConfiguration
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = string.Empty;
    public int Database { get; set; } = 0;
    public string InstanceName { get; set; } = "ByteBook";
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);
    public bool AbortOnConnectFail { get; set; } = false;
    public int ConnectRetry { get; set; } = 3;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan SyncTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public class ElasticsearchConfiguration
{
    public const string SectionName = "Elasticsearch";
    
    public string Uri { get; set; } = "http://localhost:9200";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IndexPrefix { get; set; } = "bytebook";
    public bool EnableDebugMode { get; set; } = false;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public bool ThrowExceptions { get; set; } = false;
}

public class CacheSettings
{
    public const string SectionName = "Cache";
    
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan UserSessionExpiry { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan BookContentExpiry { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan SearchResultsExpiry { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan TokenBlacklistExpiry { get; set; } = TimeSpan.FromDays(7);
    public bool EnableDistributedLocking { get; set; } = true;
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LockExpiry { get; set; } = TimeSpan.FromMinutes(5);
}