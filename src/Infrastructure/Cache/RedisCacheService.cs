using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularMonolith.Shared.Interfaces;
using StackExchange.Redis;

namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Redis-specific cache service implementation with full pattern and tag-based invalidation support
/// </summary>
internal sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _keyPrefix;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _database = connectionMultiplexer.GetDatabase(options.Value.Redis.Database);
        _server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        _options = options.Value;
        _logger = logger;
        _keyPrefix = _options.Redis.KeyPrefix;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var cachedValue = await _database.StringGetAsync(prefixedKey);
            
            if (!cachedValue.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue!, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache entry for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var expirationTime = expiration ?? _options.DefaultExpiration;

            await _database.StringSetAsync(prefixedKey, serializedValue, expirationTime);
            _logger.LogDebug("Cache entry set for key: {Key} with expiration: {Expiration}", 
                key, expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var removed = await _database.KeyDeleteAsync(prefixedKey);
            
            _logger.LogDebug("Cache entry removal for key: {Key}, Success: {Success}", key, removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
            var prefixedPattern = GetPrefixedKey(pattern);
            var keys = _server.Keys(pattern: prefixedPattern).ToArray();
            
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keys.Length, pattern);
            }
            else
            {
                _logger.LogDebug("No cache entries found matching pattern: {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        try
        {
            var tagKey = GetTagKey(tag);
            var taggedKeys = await _database.SetMembersAsync(tagKey);
            
            if (taggedKeys.Length > 0)
            {
                // Remove all keys associated with the tag
                var keysToDelete = taggedKeys.Select(k => (RedisKey)k.ToString()).ToArray();
                await _database.KeyDeleteAsync(keysToDelete);
                
                // Remove the tag set itself
                await _database.KeyDeleteAsync(tagKey);
                
                _logger.LogDebug("Removed {Count} cache entries with tag: {Tag}", taggedKeys.Length, tag);
            }
            else
            {
                _logger.LogDebug("No cache entries found with tag: {Tag}", tag);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries by tag: {Tag}", tag);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var exists = await _database.KeyExistsAsync(prefixedKey);
            
            _logger.LogDebug("Cache key existence check for {Key}: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Sets a cache entry with associated tags for tag-based invalidation
    /// </summary>
    public async Task SetWithTagsAsync<T>(string key, T value, string[] tags, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(tags);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var expirationTime = expiration ?? _options.DefaultExpiration;

            // Set the cache entry
            await _database.StringSetAsync(prefixedKey, serializedValue, expirationTime);

            // Associate the key with tags
            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var tagKey = GetTagKey(tag);
                await _database.SetAddAsync(tagKey, prefixedKey);
                // Set expiration on tag set (slightly longer than cache entries)
                await _database.KeyExpireAsync(tagKey, expirationTime.Add(TimeSpan.FromMinutes(5)));
            }

            _logger.LogDebug("Cache entry set for key: {Key} with tags: {Tags} and expiration: {Expiration}", 
                key, string.Join(", ", tags), expirationTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry with tags for key: {Key}", key);
            throw;
        }
    }

    private string GetPrefixedKey(string key) => $"{_keyPrefix}{key}";
    private string GetTagKey(string tag) => $"{_keyPrefix}tag:{tag}";
}