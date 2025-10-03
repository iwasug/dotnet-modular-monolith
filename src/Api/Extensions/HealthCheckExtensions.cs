
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModularMonolith.Infrastructure.HealthChecks;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Cache;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring health checks and metrics
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds comprehensive health checks for the application
    /// </summary>
    public static IServiceCollection AddComprehensiveHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Add database health check
        healthChecksBuilder.AddCheck<DatabaseHealthCheck>(
            name: "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "database", "infrastructure" });

        // Entity Framework health check can be added when the package is properly configured

        // Add cache health check
        healthChecksBuilder.AddCheck<CacheHealthCheck>(
            name: "cache",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "cache", "infrastructure" });

        // Redis health check can be added when the package is properly configured

        // Add memory health check
        healthChecksBuilder.AddCheck("memory", () =>
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            
            // Convert to MB for easier reading
            var workingSetMB = workingSet / (1024 * 1024);
            var privateMemoryMB = privateMemory / (1024 * 1024);
            
            var data = new Dictionary<string, object>
            {
                ["WorkingSetMB"] = workingSetMB,
                ["PrivateMemoryMB"] = privateMemoryMB,
                ["ThreadCount"] = process.Threads.Count,
                ["HandleCount"] = process.HandleCount
            };

            // Warning thresholds (adjust based on your requirements)
            if (workingSetMB > 1000) // 1GB
            {
                return HealthCheckResult.Degraded(
                    $"High memory usage: {workingSetMB}MB working set", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Memory usage normal: {workingSetMB}MB working set", 
                data: data);
        }, tags: new[] { "memory", "system" });

        // Add disk space health check
        healthChecksBuilder.AddCheck("disk_space", () =>
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .ToArray();

                var data = new Dictionary<string, object>();
                var hasLowSpace = false;
                var hasCriticalSpace = false;

                foreach (var drive in drives)
                {
                    var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    var usedPercentage = ((totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                    data[$"Drive_{drive.Name.Replace("\\", "").Replace(":", "")}"] = new
                    {
                        FreeSpaceGB = Math.Round(freeSpaceGB, 2),
                        TotalSpaceGB = Math.Round(totalSpaceGB, 2),
                        UsedPercentage = Math.Round(usedPercentage, 2)
                    };

                    if (usedPercentage > 95)
                        hasCriticalSpace = true;
                    else if (usedPercentage > 85)
                        hasLowSpace = true;
                }

                if (hasCriticalSpace)
                {
                    return HealthCheckResult.Unhealthy("Critical disk space usage detected", data: data);
                }

                if (hasLowSpace)
                {
                    return HealthCheckResult.Degraded("Low disk space detected", data: data);
                }

                return HealthCheckResult.Healthy("Disk space usage normal", data: data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded("Unable to check disk space", ex);
            }
        }, tags: new[] { "disk", "system" });

        // Health Checks UI can be added later with proper package configuration

        return services;
    }

    /// <summary>
    /// Maps health check endpoints with detailed responses
    /// </summary>
    public static void MapComprehensiveHealthChecks(this WebApplication app)
    {
        // Main health check endpoint
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        // Database-specific health check
        app.MapHealthChecks("/health/database", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database")
        });

        // Cache-specific health check
        app.MapHealthChecks("/health/cache", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("cache")
        });

        // System-specific health check
        app.MapHealthChecks("/health/system", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("system")
        });

        // Infrastructure health check
        app.MapHealthChecks("/health/infrastructure", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("infrastructure")
        });

        // Liveness probe (simple check for container orchestrators)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // No checks, just returns healthy if app is running
        });

        // Readiness probe (checks if app is ready to serve requests)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database") || check.Tags.Contains("cache")
        });
    }
}