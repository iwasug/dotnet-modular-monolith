using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Data.Migrations;

namespace ModularMonolith.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity and migration status
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public DatabaseHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Check for pending migrations
            var pendingMigrations = await MigrationValidation.GetPendingMigrationsAsync(_context, cancellationToken);
            var appliedMigrations = await MigrationValidation.GetAppliedMigrationsAsync(_context, cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["AppliedMigrations"] = appliedMigrations.Count(),
                ["PendingMigrations"] = pendingMigrations.Count(),
                ["DatabaseProvider"] = _context.Database.ProviderName ?? "Unknown"
            };

            if (pendingMigrations.Any())
            {
                data["PendingMigrationsList"] = pendingMigrations.ToArray();
                return HealthCheckResult.Degraded(
                    $"Database is accessible but has {pendingMigrations.Count()} pending migrations", 
                    data: data);
            }

            // Perform basic schema validation
            var isSchemaValid = await MigrationValidation.ValidateDatabaseSchemaAsync(
                _context, 
                Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, 
                cancellationToken);

            if (!isSchemaValid)
            {
                return HealthCheckResult.Degraded(
                    "Database is accessible but schema validation failed", 
                    data: data);
            }

            return HealthCheckResult.Healthy("Database is healthy and up to date", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}