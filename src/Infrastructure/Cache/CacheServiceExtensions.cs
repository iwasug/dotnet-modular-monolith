using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModularMonolith.Infrastructure.Performance;
using ModularMonolith.Shared.Interfaces;
using StackExchange.Redis;

namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Extension methods for registering cache services with performance optimizations
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Adds cache services to the service collection with performance monitoring
    /// </summary>
    public static IServiceCollection AddCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cache options
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        
        // Add cache performance analyzer
        services.AddSingleton<CachePerformanceAnalyzer>();
        
        switch (cacheOptions.Provider.ToLowerInvariant())
        {
            case "redis":
                services.AddOptimizedRedisCache(cacheOptions);
                break;
            case "inmemory":
            default:
                services.AddOptimizedInMemoryCache();
                break;
        }

        return services;
    }
    
    /// <summary>
    /// Adds cache warming services for preloading frequently accessed data
    /// </summary>
    public static IServiceCollection AddCacheWarming(this IServiceCollection services)
    {
        services.AddHostedService<CacheWarmupService>();
        return services;
    }

    private static IServiceCollection AddOptimizedRedisCache(this IServiceCollection services, CacheOptions cacheOptions)
    {
        // Add Redis connection with optimizations
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IConnectionMultiplexer>>();
            
            try
            {
                var connectionString = cacheOptions.Redis.ConnectionString;
                var configuration = ConfigurationOptions.Parse(connectionString);
                
                // Optimize Redis connection settings
                configuration.AbortOnConnectFail = false;
                configuration.ConnectRetry = 3;
                configuration.ConnectTimeout = 5000;
                configuration.SyncTimeout = 5000;
                configuration.KeepAlive = 60; // Keep connection alive
                configuration.DefaultDatabase = cacheOptions.Redis.Database;
                
                // Disable potentially dangerous commands in production
                configuration.CommandMap = CommandMap.Create(new HashSet<string>
                {
                    "FLUSHDB", "FLUSHALL", "KEYS", "CONFIG"
                }, available: false);
                
                var connection = ConnectionMultiplexer.Connect(configuration);
                
                connection.ConnectionFailed += (sender, args) =>
                {
                    logger.LogError("Redis connection failed: {EndPoint} - {FailureType}", 
                        args.EndPoint, args.FailureType);
                };
                
                connection.ConnectionRestored += (sender, args) =>
                {
                    logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
                };
                
                logger.LogInformation("Optimized Redis connection established successfully");
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to establish Redis connection. Falling back to in-memory cache.");
                throw;
            }
        });

        // Add Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var connectionMultiplexer = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                return Task.FromResult(connectionMultiplexer);
            };
        });

        // Register the optimized cache service with Redis support
        services.AddSingleton<ICacheService>(provider =>
        {
            var distributedCache = provider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheOptions>>();
            var logger = provider.GetRequiredService<ILogger<OptimizedCacheService>>();
            
            return new OptimizedCacheService(distributedCache, options, logger, connectionMultiplexer);
        });
        
        return services;
    }

    private static IServiceCollection AddOptimizedInMemoryCache(this IServiceCollection services)
    {
        // Add in-memory cache with optimizations
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1000; // Limit cache size
            options.CompactionPercentage = 0.25; // Compact when 25% over limit
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Scan for expired entries every 5 minutes
        });
        
        services.AddDistributedMemoryCache();
        
        // Register the optimized cache service without Redis
        services.AddSingleton<ICacheService>(provider =>
        {
            var distributedCache = provider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheOptions>>();
            var logger = provider.GetRequiredService<ILogger<OptimizedCacheService>>();
            
            return new OptimizedCacheService(distributedCache, options, logger);
        });
        
        return services;
    }
}