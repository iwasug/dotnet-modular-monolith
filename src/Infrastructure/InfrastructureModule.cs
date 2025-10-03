using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.HealthChecks;
using ModularMonolith.Infrastructure.Repositories;
using ModularMonolith.Infrastructure.Cache;
using ModularMonolith.Infrastructure.Performance;
using ModularMonolith.Infrastructure.Configuration;
using ModularMonolith.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Infrastructure;

/// <summary>
/// Infrastructure module registration for dependency injection
/// </summary>
public class InfrastructureModule : IModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register Infrastructure services
        RegisterDatabase(services);
        RegisterRepositories(services);
        RegisterHealthChecks(services);
        RegisterCacheServices(services);
        RegisterPerformanceServices(services);
        RegisterValidationServices(services);
    }

    private static void RegisterDatabase(IServiceCollection services)
    {
        // Register HttpContextAccessor for audit functionality
        services.AddHttpContextAccessor();
        
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
            
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
                
                // Enable performance optimizations
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            
            // Configure for development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            
            // Configure query tracking behavior for better performance
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            
            // Add query performance monitoring
            var queryMonitor = serviceProvider.GetService<QueryPerformanceMonitor>();
            if (queryMonitor is not null)
            {
                options.AddInterceptors(queryMonitor);
            }
        });

        // Register hosted service for automatic migration
        services.AddHostedService<DatabaseMigrationService>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        // Register generic repository and unit of work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register ApplicationDbContext as DbContext for modules to use
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
        // Specific repository registrations are now handled by each module
    }

    private static void RegisterHealthChecks(IServiceCollection services)
    {
        // Health check services are registered by the API layer
        // Just register the health check implementations here
        services.AddScoped<DatabaseHealthCheck>();
        services.AddScoped<CacheHealthCheck>();
    }

    private static void RegisterCacheServices(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        services.AddCacheServices(configuration);
        services.AddCacheWarming();
    }

    private static void RegisterPerformanceServices(IServiceCollection services)
    {
        // Register query performance monitor
        services.AddSingleton<QueryPerformanceMonitor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<QueryPerformanceMonitor>>();
            var slowQueryThreshold = TimeSpan.FromMilliseconds(1000); // 1 second threshold
            return new QueryPerformanceMonitor(logger, slowQueryThreshold);
        });
        
        // Register cache performance analyzer
        services.AddSingleton<CachePerformanceAnalyzer>();
    }

    private static void RegisterValidationServices(IServiceCollection services)
    {
        // Register configuration validator
        services.AddSingleton<ConfigurationValidator>();
        
        // Register startup validation service
        services.AddHostedService<StartupValidationService>();
        
        // Register entity discovery service
        services.AddScoped<IEntityDiscoveryService, EntityDiscoveryService>();
    }
}