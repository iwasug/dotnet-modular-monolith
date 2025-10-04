using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModularMonolith.Shared.Interfaces;
using StackExchange.Redis;

namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Optimized cache service with enhanced pattern matching, tag support, and performance improvements
/// </summary>
internal sealed class OptimizedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IDatabase? _redisDatabase;
    private readonly IServer? _redisServer;
    private readonly ILogger<OptimizedCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _keyPrefix;
    
    // In-memory tracking for pattern and tag-based invalidation when Redis is not available
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagToKeysMap = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _keyToTagsMap = new();
    private readonly object _lockObject = new();

    public OptimizedCacheService(
        IDistributedCache distributedCache,
        IOptions<CacheOptions> options,
        ILogger<OptimizedCacheService> logger,
        IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _logger = logger;
        _keyPrefix = _options.Redis.KeyPrefix;
        
        // Initialize Redis-specific components if available
        if (_connectionMultiplexer is not null)
        {
            _redisDatabase = _connectionMultiplexer.GetDatabase(_options.Redis.Database);
            _redisServer = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        }
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (cachedValue is null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache entry for key: {Key}", key);
            // Remove corrupted cache entry
            await RemoveAsync(key, cancellationToken);
            return default;
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
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cache entry set for key: {Key} with expiration: {Expiration}", 
                key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            throw;
        }
    }

    public async Task SetWithTagsAsync<T>(string key, T value, string[] tags, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(tags);

        // Set the cache entry first
        await SetAsync(key, value, expiration, cancellationToken);

        // Handle tag associations
        if (_redisDatabase is not null)
        {
            await SetRedisTagsAsync(key, tags, expiration, cancellationToken);
        }
        else
        {
            SetInMemoryTags(key, tags);
        }

        _logger.LogDebug("Cache entry set for key: {Key} with tags: {Tags}", key, string.Join(", ", tags));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            
            // Clean up tag associations
            if (_redisDatabase is null)
            {
                RemoveInMemoryTagAssociations(key);
            }
            
            _logger.LogDebug("Cache entry removed for key: {Key}", key);
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
            if (_redisDatabase is not null && _redisServer is not null)
            {
                await RemoveRedisPatternAsync(pattern, cancellationToken);
            }
            else
            {
                await RemoveInMemoryPatternAsync(pattern, cancellationToken);
            }
            
            _logger.LogDebug("Cache entries removed by pattern: {Pattern}", pattern);
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
            if (_redisDatabase is not null)
            {
                await RemoveRedisTagAsync(tag, cancellationToken);
            }
            else
            {
                await RemoveInMemoryTagAsync(tag, cancellationToken);
            }
            
            _logger.LogDebug("Cache entries removed by tag: {Tag}", tag);
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
            if (_redisDatabase is not null)
            {
                var exists = await _redisDatabase.KeyExistsAsync(GetPrefixedKey(key));
                _logger.LogDebug("Cache key existence check for {Key}: {Exists}", key, exists);
                return exists;
            }
            else
            {
                var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                var exists = cachedValue is not null;
                _logger.LogDebug("Cache key existence check for {Key}: {Exists}", key, exists);
                return exists;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task<IDictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var keyList = keys.ToList();
        var result = new Dictionary<string, T?>();

        if (_redisDatabase is not null)
        {
            // Use Redis MGET for better performance
            var redisKeys = keyList.Select(key => (RedisKey)GetPrefixedKey(key)).ToArray();
            var values = await _redisDatabase.StringGetAsync(redisKeys);
            
            for (int i = 0; i < keyList.Count; i++)
            {
                var key = keyList[i];
                var value = values[i];
                
                if (value.HasValue)
                {
                    try
                    {
                        result[key] = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
                        _logger.LogDebug("Cache hit for key: {Key}", key);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cache entry for key: {Key}", key);
                        result[key] = default;
                    }
                }
                else
                {
                    result[key] = default;
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                }
            }
        }
        else
        {
            // Fallback to individual gets
            foreach (var key in keyList)
            {
                result[key] = await GetAsync<T>(key, cancellationToken);
            }
        }

        return result;
    }

    public async Task SetMultipleAsync<T>(IDictionary<string, T> keyValuePairs, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_redisDatabase is not null)
        {
            // Use Redis pipeline for better performance
            var batch = _redisDatabase.CreateBatch();
            var tasks = new List<Task>();
            var expirationTime = expiration ?? _options.DefaultExpiration;

            foreach (var kvp in keyValuePairs)
            {
                var serializedValue = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
                var task = batch.StringSetAsync(GetPrefixedKey(kvp.Key), serializedValue, expirationTime);
                tasks.Add(task);
            }

            batch.Execute();
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Set {Count} cache entries in batch", keyValuePairs.Count);
        }
        else
        {
            // Fallback to individual sets
            var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SetRedisTagsAsync(string key, string[] tags, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        if (_redisDatabase is null) return;

        var expirationTime = expiration ?? _options.DefaultExpiration;
        var prefixedKey = GetPrefixedKey(key);
        
        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            var tagKey = GetTagKey(tag);
            await _redisDatabase.SetAddAsync(tagKey, prefixedKey);
            // Set expiration on tag set (slightly longer than cache entries)
            await _redisDatabase.KeyExpireAsync(tagKey, expirationTime.Add(TimeSpan.FromMinutes(5)));
        }
    }

    private void SetInMemoryTags(string key, string[] tags)
    {
        lock (_lockObject)
        {
            // Clean up existing associations for this key
            if (_keyToTagsMap.TryGetValue(key, out var existingTags))
            {
                foreach (var existingTag in existingTags)
                {
                    if (_tagToKeysMap.TryGetValue(existingTag, out var keys))
                    {
                        keys.Remove(key);
                        if (keys.Count == 0)
                        {
                            _tagToKeysMap.TryRemove(existingTag, out _);
                        }
                    }
                }
            }

            // Set new associations
            var tagSet = new HashSet<string>(tags.Where(t => !string.IsNullOrWhiteSpace(t)));
            _keyToTagsMap[key] = tagSet;

            foreach (var tag in tagSet)
            {
                _tagToKeysMap.AddOrUpdate(tag, 
                    new HashSet<string> { key },
                    (_, existing) => { existing.Add(key); return existing; });
            }
        }
    }

    private async Task RemoveRedisPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        if (_redisServer is null || _redisDatabase is null) return;

        var prefixedPattern = GetPrefixedKey(pattern);
        var keys = _redisServer.Keys(pattern: prefixedPattern).ToArray();
        
        if (keys.Length > 0)
        {
            await _redisDatabase.KeyDeleteAsync(keys);
            _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keys.Length, pattern);
        }
    }

    private async Task RemoveInMemoryPatternAsync(string pattern, CancellationToken cancellationToken)
    {
        // This is a simplified pattern matching - in production you might want more sophisticated pattern matching
        var keysToRemove = new List<string>();
        
        lock (_lockObject)
        {
            var simplePattern = pattern.Replace("*", "");
            keysToRemove.AddRange(_keyToTagsMap.Keys.Where(k => k.Contains(simplePattern)));
        }

        foreach (var key in keysToRemove)
        {
            await RemoveAsync(key, cancellationToken);
        }
        
        _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
    }

    private async Task RemoveRedisTagAsync(string tag, CancellationToken cancellationToken)
    {
        if (_redisDatabase is null) return;

        var tagKey = GetTagKey(tag);
        var taggedKeys = await _redisDatabase.SetMembersAsync(tagKey);
        
        if (taggedKeys.Length > 0)
        {
            var keysToDelete = taggedKeys.Select(k => (RedisKey)k.ToString()).ToArray();
            await _redisDatabase.KeyDeleteAsync(keysToDelete);
            await _redisDatabase.KeyDeleteAsync(tagKey);
            
            _logger.LogDebug("Removed {Count} cache entries with tag: {Tag}", taggedKeys.Length, tag);
        }
    }

    private async Task RemoveInMemoryTagAsync(string tag, CancellationToken cancellationToken)
    {
        var keysToRemove = new List<string>();
        
        lock (_lockObject)
        {
            if (_tagToKeysMap.TryRemove(tag, out var keys))
            {
                keysToRemove.AddRange(keys);
                
                // Clean up reverse mappings
                foreach (var key in keys)
                {
                    if (_keyToTagsMap.TryGetValue(key, out var tags))
                    {
                        tags.Remove(tag);
                        if (tags.Count == 0)
                        {
                            _keyToTagsMap.TryRemove(key, out _);
                        }
                    }
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            await RemoveAsync(key, cancellationToken);
        }
        
        _logger.LogDebug("Removed {Count} cache entries with tag: {Tag}", keysToRemove.Count, tag);
    }

    private void RemoveInMemoryTagAssociations(string key)
    {
        lock (_lockObject)
        {
            if (_keyToTagsMap.TryRemove(key, out var tags))
            {
                foreach (var tag in tags)
                {
                    if (_tagToKeysMap.TryGetValue(tag, out var keys))
                    {
                        keys.Remove(key);
                        if (keys.Count == 0)
                        {
                            _tagToKeysMap.TryRemove(tag, out _);
                        }
                    }
                }
            }
        }
    }

    private string GetPrefixedKey(string key) => $"{_keyPrefix}{key}";
    private string GetTagKey(string tag) => $"{_keyPrefix}tag:{tag}";
}