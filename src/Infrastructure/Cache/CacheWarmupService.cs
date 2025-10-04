using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Roles.Domain;

namespace ModularMonolith.Infrastructure.Cache;

/// <summary>
/// Background service for warming up cache with frequently accessed data
/// </summary>
public sealed class CacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly TimeSpan _warmupInterval = TimeSpan.FromHours(6); // Warm up every 6 hours

    public CacheWarmupService(IServiceProvider serviceProvider, ILogger<CacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial warmup after a short delay
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WarmupCache(stoppingToken);
                await Task.Delay(_warmupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task WarmupCache(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache warmup process");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var scope = _serviceProvider.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        try
        {
            // Warm up frequently accessed data
            await WarmupActiveUsers(scope.ServiceProvider, cacheService, cancellationToken);
            await WarmupActiveRoles(scope.ServiceProvider, cacheService, cancellationToken);
            await WarmupSystemMetrics(scope.ServiceProvider, cacheService, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Cache warmup completed successfully in {Duration}ms", 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Cache warmup failed after {Duration}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task WarmupActiveUsers(IServiceProvider serviceProvider, ICacheService cacheService, CancellationToken cancellationToken)
    {
        try
        {
            var userRepository = serviceProvider.GetService<IUserRepository>();
            if (userRepository is null) return;

            _logger.LogDebug("Warming up active users cache");

            // Cache active users count
            var activeUserCount = await userRepository.GetActiveCountAsync(cancellationToken);
            await cacheService.SetAsync("users:count:active", activeUserCount, TimeSpan.FromHours(1), cancellationToken);

            // Cache total users count
            var totalUserCount = await userRepository.GetCountAsync(cancellationToken);
            await cacheService.SetAsync("users:count", totalUserCount, TimeSpan.FromHours(1), cancellationToken);

            // Cache first page of active users (most commonly accessed)
            var activeUsers = await userRepository.GetActiveUsersAsync(cancellationToken);
            var firstPageUsers = activeUsers.Take(20).ToList(); // First 20 users
            await cacheService.SetAsync("users:active", firstPageUsers, TimeSpan.FromMinutes(30), cancellationToken);

            _logger.LogDebug("Warmed up {UserCount} active users in cache", firstPageUsers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm up users cache");
        }
    }

    private async Task WarmupActiveRoles(IServiceProvider serviceProvider, ICacheService cacheService, CancellationToken cancellationToken)
    {
        try
        {
            var roleRepository = serviceProvider.GetService<IRoleRepository>();
            if (roleRepository is null) return;

            _logger.LogDebug("Warming up active roles cache");

            // Cache active roles (typically small dataset)
            var activeRoles = await roleRepository.GetActiveRolesAsync(cancellationToken);
            await cacheService.SetAsync("roles:active", activeRoles, TimeSpan.FromHours(2), cancellationToken);

            // Cache all roles (for role management operations)
            var allRoles = await roleRepository.GetAllAsync(cancellationToken);
            await cacheService.SetAsync("roles:all", allRoles, TimeSpan.FromHours(1), cancellationToken);

            // Cache individual roles by ID (most frequently accessed)
            foreach (var role in activeRoles.Take(10)) // Top 10 roles
            {
                var cacheKey = $"role:id:{role.Id}";
                await cacheService.SetAsync(cacheKey, role, TimeSpan.FromHours(1), cancellationToken);
            }

            _logger.LogDebug("Warmed up {RoleCount} active roles in cache", activeRoles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm up roles cache");
        }
    }

    private async Task WarmupSystemMetrics(IServiceProvider serviceProvider, ICacheService cacheService, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Warming up system metrics cache");

            // Cache system health status
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            };
            await cacheService.SetAsync("system:health", healthStatus, TimeSpan.FromMinutes(5), cancellationToken);

            // Cache application metadata
            var appMetadata = new
            {
                Name = "ModularMonolith API",
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                StartTime = DateTime.UtcNow
            };
            await cacheService.SetAsync("system:metadata", appMetadata, TimeSpan.FromHours(24), cancellationToken);

            _logger.LogDebug("Warmed up system metrics in cache");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to warm up system metrics cache");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cache warmup service is stopping");
        await base.StopAsync(cancellationToken);
    }
}