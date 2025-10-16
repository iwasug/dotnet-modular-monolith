using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Cache service implementation with support for both Redis and In-Memory providers
/// </summary>
internal sealed class CacheService(
    IDistributedCache distributedCache,
    IOptions<CacheOptions> options,
    ILogger<CacheService> logger)
    : ICacheService
{
    private readonly CacheOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedValue = await distributedCache.GetStringAsync(key, cancellationToken);
            
            if (cachedValue is null)
            {
                logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving cache entry for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration
            };

            await distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            logger.LogDebug("Cache entry set for key: {Key} with expiration: {Expiration}", 
                key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await distributedCache.RemoveAsync(key, cancellationToken);
            logger.LogDebug("Cache entry removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            throw;
        }
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            // For distributed cache, we need to implement pattern-based removal
            // This is a simplified implementation - in production, you might want to use Redis-specific commands
            logger.LogWarning("Pattern-based cache removal is not fully supported with IDistributedCache. Pattern: {Pattern}", pattern);
            
            // Note: This is a limitation of IDistributedCache interface
            // For full Redis pattern support, you would need to use IDatabase directly
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        try
        {
            // For distributed cache, we need to implement tag-based removal
            // This is a simplified implementation - in production, you might want to use Redis-specific commands
            logger.LogWarning("Tag-based cache removal is not fully supported with IDistributedCache. Tag: {Tag}", tag);
            
            // Note: This is a limitation of IDistributedCache interface
            // For full Redis tag support, you would need to use IDatabase directly or implement a tag tracking system
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing cache entries by tag: {Tag}", tag);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedValue = await distributedCache.GetStringAsync(key, cancellationToken);
            var exists = cachedValue is not null;
            
            logger.LogDebug("Cache key existence check for {Key}: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }
}