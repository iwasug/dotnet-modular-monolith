using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Infrastructure.Data.Migrations;

/// <summary>
/// Utility class to validate database migrations and schema
/// </summary>
public static class MigrationValidation
{
    /// <summary>
    /// Validates that all required tables and indexes exist in the database
    /// </summary>
    public static async Task<bool> ValidateDatabaseSchemaAsync(
        ApplicationDbContext context, 
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting database schema validation");

            // Check if database exists and can be connected to
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                logger.LogError("Cannot connect to database");
                return false;
            }

            // Check if all required tables exist
            var requiredTables = new[]
            {
                "Users",
                "Roles", 
                "Permissions",
                "UserRoles",
                "RolePermissions",
                "RefreshTokens"
            };

            foreach (var tableName in requiredTables)
            {
                var tableExists = await TableExistsAsync(context, tableName, cancellationToken);
                if (!tableExists)
                {
                    logger.LogError("Required table {TableName} does not exist", tableName);
                    return false;
                }
                logger.LogDebug("Table {TableName} exists", tableName);
            }

            // Check if critical indexes exist
            var criticalIndexes = new[]
            {
                ("Users", "IX_Users_Email"),
                ("Roles", "IX_Roles_Name"),
                ("Permissions", "IX_Permissions_Resource_Action_Scope"),
                ("UserRoles", "IX_UserRoles_UserId_RoleId"),
                ("RolePermissions", "IX_RolePermissions_RoleId_PermissionId"),
                ("RefreshTokens", "IX_RefreshTokens_Token_Active")
            };

            foreach (var (tableName, indexName) in criticalIndexes)
            {
                var indexExists = await IndexExistsAsync(context, tableName, indexName, cancellationToken);
                if (!indexExists)
                {
                    logger.LogWarning("Critical index {IndexName} on table {TableName} does not exist", indexName, tableName);
                    // Don't fail validation for missing indexes, just warn
                }
                else
                {
                    logger.LogDebug("Index {IndexName} on table {TableName} exists", indexName, tableName);
                }
            }

            logger.LogInformation("Database schema validation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database schema validation");
            return false;
        }
    }

    /// <summary>
    /// Checks if a table exists in the database
    /// </summary>
    private static async Task<bool> TableExistsAsync(
        ApplicationDbContext context, 
        string tableName, 
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = """
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = {0}
                );
                """;

            var result = await context.Database
                .SqlQueryRaw<bool>(sql, tableName.ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            return result;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an index exists on a table
    /// </summary>
    private static async Task<bool> IndexExistsAsync(
        ApplicationDbContext context, 
        string tableName, 
        string indexName, 
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = """
                SELECT EXISTS (
                    SELECT FROM pg_indexes 
                    WHERE schemaname = 'public' 
                    AND tablename = {0}
                    AND indexname = {1}
                );
                """;

            var result = await context.Database
                .SqlQueryRaw<bool>(sql, tableName.ToLower(), indexName.ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            return result;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the list of applied migrations
    /// </summary>
    public static async Task<IEnumerable<string>> GetAppliedMigrationsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the list of pending migrations
    /// </summary>
    public static async Task<IEnumerable<string>> GetPendingMigrationsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Database.GetPendingMigrationsAsync(cancellationToken);
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}