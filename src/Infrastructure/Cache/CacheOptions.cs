namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Configuration options for caching
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";
    
    /// <summary>
    /// Cache provider type (InMemory or Redis)
    /// </summary>
    public string Provider { get; set; } = "InMemory";
    
    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Redis configuration options
    /// </summary>
    public RedisOptions Redis { get; set; } = new();
}

/// <summary>
/// Redis-specific configuration options
/// </summary>
public sealed class RedisOptions
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";
    
    /// <summary>
    /// Key prefix for all cache entries
    /// </summary>
    public string KeyPrefix { get; set; } = "mm:";
    
    /// <summary>
    /// Database number to use
    /// </summary>
    public int Database { get; set; } = 0;
}