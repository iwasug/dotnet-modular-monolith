using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModularMonolith.Infrastructure.Configuration;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ModularMonolith.Infrastructure.Services;

/// <summary>
/// Service that validates system readiness at startup
/// </summary>
public sealed class StartupValidationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupValidationService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public StartupValidationService(
        IServiceProvider serviceProvider,
        ILogger<StartupValidationService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting application validation");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            // Validate configuration
            await ValidateConfiguration(scope.ServiceProvider);
            
            // Validate database connectivity
            await ValidateDatabase(scope.ServiceProvider, cancellationToken);
            
            // Validate cache connectivity
            await ValidateCache(scope.ServiceProvider, cancellationToken);
            
            // Validate all required services are registered
            ValidateServiceRegistrations(scope.ServiceProvider);
            
            // Log configuration summary
            LogConfigurationSummary(scope.ServiceProvider);

            _logger.LogInformation("Application validation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application validation failed - shutting down");
            _applicationLifetime.StopApplication();
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Startup validation service is stopping");
        return Task.CompletedTask;
    }

    private async Task ValidateConfiguration(IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Validating configuration");
        
        var configValidator = serviceProvider.GetRequiredService<ConfigurationValidator>();
        var validationResult = configValidator.ValidateConfiguration();
        
        if (validationResult != ValidationResult.Success)
        {
            throw new InvalidOperationException($"Configuration validation failed: {validationResult.ErrorMessage}");
        }
        
        _logger.LogDebug("Configuration validation passed");
    }

    private async Task ValidateDatabase(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating database connectivity");
        
        try
        {
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Test database connectivity
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                throw new InvalidOperationException("Cannot connect to database");
            }
            
            // Check if database exists and has required tables
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                _logger.LogWarning("Database has {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count(), string.Join(", ", pendingMigrations));
            }
            
            // Test a simple query
            var userCount = await dbContext.Users.CountAsync(cancellationToken);
            _logger.LogDebug("Database connectivity validated - Users table has {Count} records", userCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database validation failed");
            throw new InvalidOperationException("Database validation failed", ex);
        }
    }

    private async Task ValidateCache(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating cache connectivity");
        
        try
        {
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();
            
            // Test cache connectivity with a simple set/get operation
            var testKey = "startup-validation-test";
            var testValue = DateTime.UtcNow.ToString();
            
            await cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1), cancellationToken);
            var retrievedValue = await cacheService.GetAsync<string>(testKey, cancellationToken);
            
            if (retrievedValue != testValue)
            {
                throw new InvalidOperationException("Cache set/get operation failed");
            }
            
            // Clean up test key
            await cacheService.RemoveAsync(testKey, cancellationToken);
            
            _logger.LogDebug("Cache connectivity validated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache validation failed");
            throw new InvalidOperationException("Cache validation failed", ex);
        }
    }

    private void ValidateServiceRegistrations(IServiceProvider serviceProvider)
    {
        _logger.LogDebug("Validating service registrations");
        
        var requiredServices = new[]
        {
            typeof(ApplicationDbContext),
            typeof(ICacheService),
            typeof(ITimeService),
            typeof(ConfigurationValidator)
        };

        var missingServices = new List<string>();
        
        foreach (var serviceType in requiredServices)
        {
            try
            {
                var service = serviceProvider.GetService(serviceType);
                if (service is null)
                {
                    missingServices.Add(serviceType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve service {ServiceType}", serviceType.Name);
                missingServices.Add(serviceType.Name);
            }
        }

        if (missingServices.Count > 0)
        {
            throw new InvalidOperationException($"Required services are not registered: {string.Join(", ", missingServices)}");
        }
        
        _logger.LogDebug("Service registration validation passed");
    }

    private void LogConfigurationSummary(IServiceProvider serviceProvider)
    {
        try
        {
            var configValidator = serviceProvider.GetRequiredService<ConfigurationValidator>();
            var summary = configValidator.GetConfigurationSummary();
            
            _logger.LogInformation("Application Configuration Summary:");
            _logger.LogInformation("  Environment: {Environment}", summary.Environment);
            _logger.LogInformation("  Database: {DatabaseProvider}", summary.DatabaseProvider);
            _logger.LogInformation("  Cache: {CacheProvider}", summary.CacheProvider);
            _logger.LogInformation("  JWT Configured: {JwtConfigured}", summary.JwtConfigured);
            _logger.LogInformation("  Logging Configured: {LoggingConfigured}", summary.LoggingConfigured);
            _logger.LogInformation("  CORS Configured: {CorsConfigured}", summary.CorsConfigured);
            _logger.LogInformation("  Health Checks: {HealthChecksEnabled}", summary.HealthChecksEnabled);
            _logger.LogInformation("  Swagger: {SwaggerEnabled}", summary.SwaggerEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate configuration summary");
        }
    }
}