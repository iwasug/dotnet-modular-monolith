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
public class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting database migration service");

        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // First, ensure database can be connected to
            logger.LogInformation("Testing database connection");
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                logger.LogError("Failed to connect to database. Migration aborted");
                throw new InvalidOperationException("Database connection failed");
            }

            logger.LogInformation("Database connection verified successfully");

            // Check for pending migrations
            logger.LogInformation("Checking for pending database migrations");
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
            
            logger.LogInformation("Applied migrations: {AppliedCount}", appliedMigrations.Count());
            foreach (var migration in appliedMigrations)
            {
                logger.LogDebug("Applied migration: {MigrationName}", migration);
            }

            if (pendingMigrations.Any())
            {
                logger.LogInformation("Found {Count} pending migrations. Applying migrations...", 
                    pendingMigrations.Count());

                foreach (var migration in pendingMigrations)
                {
                    logger.LogInformation("Pending migration: {MigrationName}", migration);
                }

                // Apply migrations with timeout
                using var migrationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                migrationCts.CancelAfter(TimeSpan.FromMinutes(5)); // 5-minute timeout for migrations

                await dbContext.Database.MigrateAsync(migrationCts.Token);
                
                logger.LogInformation("Database migrations applied successfully");

                // Validate schema after migration
                var isValid = await MigrationValidation.ValidateDatabaseSchemaAsync(
                    dbContext, logger, cancellationToken);
                
                if (isValid)
                {
                    logger.LogInformation("Database schema validation passed");
                }
                else
                {
                    logger.LogWarning("Database schema validation failed - some issues detected");
                }

                // Seed initial data after migrations
                await SeedDataAsync(scope, cancellationToken);
            }
            else
            {
                logger.LogInformation("No pending migrations found. Database is up to date");
                
                // Still validate schema for existing database
                var isValid = await MigrationValidation.ValidateDatabaseSchemaAsync(
                    dbContext, logger, cancellationToken);
                
                if (isValid)
                {
                    logger.LogInformation("Database schema validation passed");
                }
                else
                {
                    logger.LogWarning("Database schema validation failed - some issues detected");
                }

                // Seed initial data if needed
                await SeedDataAsync(scope, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Database migration was cancelled due to timeout");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during database migration");
            throw; // Re-throw to prevent application startup if migrations fail
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Database migration service stopped");
        return Task.CompletedTask;
    }

    private async Task SeedDataAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();
            var seeder = new DataSeeder(dbContext, seederLogger);
            
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while seeding data");
            // Don't throw - seeding failure shouldn't prevent application startup
        }
    }
}