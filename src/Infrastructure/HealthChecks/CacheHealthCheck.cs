using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Infrastructure.HealthChecks;

/// <summary>
/// Health check for cache service connectivity and performance
/// </summary>
public sealed class CacheHealthCheck(ICacheService cacheService) : IHealthCheck
{
    private const string TestKey = "health-check-test";
    private const string TestValue = "test-value";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Test cache write operation
            await cacheService.SetAsync(TestKey, TestValue, TimeSpan.FromMinutes(1), cancellationToken);
            
            // Test cache read operation
            var retrievedValue = await cacheService.GetAsync<string>(TestKey, cancellationToken);
            
            // Test cache delete operation
            await cacheService.RemoveAsync(TestKey, cancellationToken);
            
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms",
                ["CacheType"] = cacheService.GetType().Name
            };

            // Verify the cache operations worked correctly
            if (retrievedValue != TestValue)
            {
                return HealthCheckResult.Degraded(
                    "Cache is accessible but data integrity check failed", 
                    data: data);
            }

            // Check if response time is acceptable (under 100ms for cache operations)
            if (stopwatch.ElapsedMilliseconds > 100)
            {
                data["Warning"] = "Cache response time is slower than expected";
                return HealthCheckResult.Degraded(
                    $"Cache is working but response time is slow ({stopwatch.ElapsedMilliseconds}ms)", 
                    data: data);
            }

            return HealthCheckResult.Healthy("Cache is healthy and responsive", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache health check failed", ex, new Dictionary<string, object>
            {
                ["CacheType"] = cacheService.GetType().Name,
                ["Error"] = ex.Message
            });
        }
    }
}