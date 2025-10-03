using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ModularMonolith.Infrastructure.Performance;

/// <summary>
/// Service for analyzing cache performance and providing optimization recommendations
/// </summary>
public sealed class CachePerformanceAnalyzer
{
    private readonly ILogger<CachePerformanceAnalyzer> _logger;
    private readonly ConcurrentDictionary<string, CacheKeyMetrics> _keyMetrics = new();
    private readonly ConcurrentDictionary<string, CachePatternMetrics> _patternMetrics = new();
    private readonly Timer _reportingTimer;

    public CachePerformanceAnalyzer(ILogger<CachePerformanceAnalyzer> logger)
    {
        _logger = logger;
        
        // Report cache metrics every 5 minutes
        _reportingTimer = new Timer(ReportMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Records a cache hit for the specified key
    /// </summary>
    public void RecordCacheHit(string key, TimeSpan retrievalTime)
    {
        var pattern = ExtractPattern(key);
        
        _keyMetrics.AddOrUpdate(key, 
            new CacheKeyMetrics { Key = key, Pattern = pattern, Hits = 1, TotalRetrievalTime = retrievalTime },
            (_, existing) => 
            {
                existing.Hits++;
                existing.TotalRetrievalTime = existing.TotalRetrievalTime.Add(retrievalTime);
                existing.LastAccessTime = DateTime.UtcNow;
                return existing;
            });

        _patternMetrics.AddOrUpdate(pattern,
            new CachePatternMetrics { Pattern = pattern, Hits = 1, TotalRetrievalTime = retrievalTime },
            (_, existing) =>
            {
                existing.Hits++;
                existing.TotalRetrievalTime = existing.TotalRetrievalTime.Add(retrievalTime);
                return existing;
            });
    }

    /// <summary>
    /// Records a cache miss for the specified key
    /// </summary>
    public void RecordCacheMiss(string key, TimeSpan fallbackTime)
    {
        var pattern = ExtractPattern(key);
        
        _keyMetrics.AddOrUpdate(key,
            new CacheKeyMetrics { Key = key, Pattern = pattern, Misses = 1, TotalFallbackTime = fallbackTime },
            (_, existing) =>
            {
                existing.Misses++;
                existing.TotalFallbackTime = existing.TotalFallbackTime.Add(fallbackTime);
                existing.LastAccessTime = DateTime.UtcNow;
                return existing;
            });

        _patternMetrics.AddOrUpdate(pattern,
            new CachePatternMetrics { Pattern = pattern, Misses = 1, TotalFallbackTime = fallbackTime },
            (_, existing) =>
            {
                existing.Misses++;
                existing.TotalFallbackTime = existing.TotalFallbackTime.Add(fallbackTime);
                return existing;
            });
    }

    /// <summary>
    /// Records cache invalidation for the specified key or pattern
    /// </summary>
    public void RecordCacheInvalidation(string keyOrPattern, int keysInvalidated = 1)
    {
        var pattern = ExtractPattern(keyOrPattern);
        
        _patternMetrics.AddOrUpdate(pattern,
            new CachePatternMetrics { Pattern = pattern, Invalidations = keysInvalidated },
            (_, existing) =>
            {
                existing.Invalidations += keysInvalidated;
                return existing;
            });
    }

    /// <summary>
    /// Gets cache performance metrics for a specific key
    /// </summary>
    public CacheKeyMetrics? GetKeyMetrics(string key)
    {
        return _keyMetrics.TryGetValue(key, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets cache performance metrics for a specific pattern
    /// </summary>
    public CachePatternMetrics? GetPatternMetrics(string pattern)
    {
        return _patternMetrics.TryGetValue(pattern, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets the top performing cache keys by hit ratio
    /// </summary>
    public IEnumerable<CacheKeyMetrics> GetTopPerformingKeys(int count = 10)
    {
        return _keyMetrics.Values
            .Where(m => m.TotalAccesses > 0)
            .OrderByDescending(m => m.HitRatio)
            .ThenByDescending(m => m.TotalAccesses)
            .Take(count);
    }

    /// <summary>
    /// Gets the worst performing cache keys by hit ratio
    /// </summary>
    public IEnumerable<CacheKeyMetrics> GetWorstPerformingKeys(int count = 10)
    {
        return _keyMetrics.Values
            .Where(m => m.TotalAccesses > 10) // Only consider keys with significant usage
            .OrderBy(m => m.HitRatio)
            .ThenByDescending(m => m.TotalAccesses)
            .Take(count);
    }

    /// <summary>
    /// Gets cache optimization recommendations
    /// </summary>
    public IEnumerable<CacheOptimizationRecommendation> GetOptimizationRecommendations()
    {
        var recommendations = new List<CacheOptimizationRecommendation>();

        // Analyze patterns with low hit ratios
        var lowHitRatioPatterns = _patternMetrics.Values
            .Where(p => p.TotalAccesses > 50 && p.HitRatio < 0.5)
            .OrderBy(p => p.HitRatio);

        foreach (var pattern in lowHitRatioPatterns)
        {
            recommendations.Add(new CacheOptimizationRecommendation
            {
                Type = OptimizationType.LowHitRatio,
                Pattern = pattern.Pattern,
                Description = $"Pattern '{pattern.Pattern}' has low hit ratio ({pattern.HitRatio:P2}). Consider reviewing cache expiration or key generation strategy.",
                Priority = pattern.HitRatio < 0.2 ? RecommendationPriority.High : RecommendationPriority.Medium,
                Metrics = pattern
            });
        }

        // Analyze patterns with high invalidation rates
        var highInvalidationPatterns = _patternMetrics.Values
            .Where(p => p.TotalAccesses > 20 && p.InvalidationRatio > 0.3)
            .OrderByDescending(p => p.InvalidationRatio);

        foreach (var pattern in highInvalidationPatterns)
        {
            recommendations.Add(new CacheOptimizationRecommendation
            {
                Type = OptimizationType.HighInvalidationRate,
                Pattern = pattern.Pattern,
                Description = $"Pattern '{pattern.Pattern}' has high invalidation rate ({pattern.InvalidationRatio:P2}). Consider optimizing cache invalidation strategy.",
                Priority = pattern.InvalidationRatio > 0.5 ? RecommendationPriority.High : RecommendationPriority.Medium,
                Metrics = pattern
            });
        }

        // Analyze unused cache keys
        var unusedKeys = _keyMetrics.Values
            .Where(k => k.LastAccessTime < DateTime.UtcNow.AddHours(-24) && k.TotalAccesses < 5)
            .GroupBy(k => k.Pattern)
            .Where(g => g.Count() > 10);

        foreach (var group in unusedKeys)
        {
            recommendations.Add(new CacheOptimizationRecommendation
            {
                Type = OptimizationType.UnusedKeys,
                Pattern = group.Key,
                Description = $"Pattern '{group.Key}' has {group.Count()} unused keys. Consider implementing cache cleanup or reducing cache duration.",
                Priority = RecommendationPriority.Low,
                Metrics = null
            });
        }

        return recommendations.OrderByDescending(r => r.Priority);
    }

    private void ReportMetrics(object? state)
    {
        try
        {
            var totalKeys = _keyMetrics.Count;
            var totalPatterns = _patternMetrics.Count;
            
            if (totalKeys == 0 && totalPatterns == 0)
            {
                return; // No metrics to report
            }

            var overallHitRatio = CalculateOverallHitRatio();
            var topPatterns = _patternMetrics.Values
                .OrderByDescending(p => p.TotalAccesses)
                .Take(5)
                .ToList();

            _logger.LogInformation("Cache Performance Report - Keys: {TotalKeys}, Patterns: {TotalPatterns}, Overall Hit Ratio: {HitRatio:P2}",
                totalKeys, totalPatterns, overallHitRatio);

            foreach (var pattern in topPatterns)
            {
                _logger.LogInformation("Top Pattern: {Pattern} - Accesses: {Accesses}, Hit Ratio: {HitRatio:P2}, Avg Retrieval: {AvgRetrieval}ms",
                    pattern.Pattern,
                    pattern.TotalAccesses,
                    pattern.HitRatio,
                    pattern.AverageRetrievalTime.TotalMilliseconds);
            }

            var recommendations = GetOptimizationRecommendations().Take(3);
            foreach (var recommendation in recommendations)
            {
                _logger.LogInformation("Cache Optimization Recommendation ({Priority}): {Description}",
                    recommendation.Priority, recommendation.Description);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cache performance report");
        }
    }

    private double CalculateOverallHitRatio()
    {
        var totalHits = _patternMetrics.Values.Sum(p => p.Hits);
        var totalMisses = _patternMetrics.Values.Sum(p => p.Misses);
        var totalAccesses = totalHits + totalMisses;
        
        return totalAccesses > 0 ? (double)totalHits / totalAccesses : 0;
    }

    private static string ExtractPattern(string key)
    {
        // Extract pattern by replacing specific IDs with placeholders
        var pattern = key;
        
        // Replace GUIDs with placeholder
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, 
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", 
            "{id}");
        
        // Replace numbers with placeholder
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, @"\d+", "{num}");
        
        // Replace email addresses with placeholder
        pattern = System.Text.RegularExpressions.Regex.Replace(pattern, 
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", 
            "{email}");
        
        return pattern;
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }
}

/// <summary>
/// Metrics for a specific cache key
/// </summary>
public sealed class CacheKeyMetrics
{
    public string Key { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public long Hits { get; set; }
    public long Misses { get; set; }
    public TimeSpan TotalRetrievalTime { get; set; }
    public TimeSpan TotalFallbackTime { get; set; }
    public DateTime LastAccessTime { get; set; } = DateTime.UtcNow;
    
    public long TotalAccesses => Hits + Misses;
    public double HitRatio => TotalAccesses > 0 ? (double)Hits / TotalAccesses : 0;
    public TimeSpan AverageRetrievalTime => Hits > 0 ? TimeSpan.FromTicks(TotalRetrievalTime.Ticks / Hits) : TimeSpan.Zero;
    public TimeSpan AverageFallbackTime => Misses > 0 ? TimeSpan.FromTicks(TotalFallbackTime.Ticks / Misses) : TimeSpan.Zero;
}

/// <summary>
/// Metrics for a cache pattern
/// </summary>
public sealed class CachePatternMetrics
{
    public string Pattern { get; set; } = string.Empty;
    public long Hits { get; set; }
    public long Misses { get; set; }
    public long Invalidations { get; set; }
    public TimeSpan TotalRetrievalTime { get; set; }
    public TimeSpan TotalFallbackTime { get; set; }
    
    public long TotalAccesses => Hits + Misses;
    public double HitRatio => TotalAccesses > 0 ? (double)Hits / TotalAccesses : 0;
    public double InvalidationRatio => TotalAccesses > 0 ? (double)Invalidations / TotalAccesses : 0;
    public TimeSpan AverageRetrievalTime => Hits > 0 ? TimeSpan.FromTicks(TotalRetrievalTime.Ticks / Hits) : TimeSpan.Zero;
    public TimeSpan AverageFallbackTime => Misses > 0 ? TimeSpan.FromTicks(TotalFallbackTime.Ticks / Misses) : TimeSpan.Zero;
}

/// <summary>
/// Cache optimization recommendation
/// </summary>
public sealed class CacheOptimizationRecommendation
{
    public OptimizationType Type { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
    public CachePatternMetrics? Metrics { get; set; }
}

public enum OptimizationType
{
    LowHitRatio,
    HighInvalidationRate,
    UnusedKeys,
    SlowRetrieval,
    MemoryUsage
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3
}