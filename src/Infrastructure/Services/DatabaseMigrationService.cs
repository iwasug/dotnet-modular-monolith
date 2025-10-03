using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Data.Migrations;

namespace ModularMonolith.Infrastructure;

/// <summary>
/// Background service that automatically applies database migrations on startup
/// </summary>
public class DatabaseMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting database migration service");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // First, ensure database can be connected to
            _logger.LogInformation("Testing database connection");
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogError("Failed to connect to database. Migration aborted");
                throw new InvalidOperationException("Database connection failed");
            }

            _logger.LogInformation("Database connection verified successfully");

            // Check for pending migrations
            _logger.LogInformation("Checking for pending database migrations");
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            
            _logger.LogInformation("Applied migrations: {AppliedCount}", appliedMigrations.Count());
            foreach (var migration in appliedMigrations)
            {
                _logger.LogDebug("Applied migration: {MigrationName}", migration);
            }

            if (pendingMigrations.Any())
            {
                _logger.LogInformation("Found {Count} pending migrations. Applying migrations...", 
                    pendingMigrations.Count());

                foreach (var migration in pendingMigrations)
                {
                    _logger.LogInformation("Pending migration: {MigrationName}", migration);
                }

                // Apply migrations with timeout
                using var migrationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                migrationCts.CancelAfter(TimeSpan.FromMinutes(5)); // 5-minute timeout for migrations

                await dbContext.Database.MigrateAsync(migrationCts.Token);
                
                _logger.LogInformation("Database migrations applied successfully");

                // Validate schema after migration
                var isValid = await MigrationValidation.ValidateDatabaseSchemaAsync(
                    dbContext, _logger, cancellationToken);
                
                if (isValid)
                {
                    _logger.LogInformation("Database schema validation passed");
                }
                else
                {
                    _logger.LogWarning("Database schema validation failed - some issues detected");
                }
            }
            else
            {
                _logger.LogInformation("No pending migrations found. Database is up to date");
                
                // Still validate schema for existing database
                var isValid = await MigrationValidation.ValidateDatabaseSchemaAsync(
                    dbContext, _logger, cancellationToken);
                
                if (isValid)
                {
                    _logger.LogInformation("Database schema validation passed");
                }
                else
                {
                    _logger.LogWarning("Database schema validation failed - some issues detected");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Database migration was cancelled due to timeout");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database migration");
            throw; // Re-throw to prevent application startup if migrations fail
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database migration service stopped");
        return Task.CompletedTask;
    }
}