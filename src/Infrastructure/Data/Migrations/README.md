# Database Migrations

This directory contains Entity Framework Core migrations for the ModularMonolith application.

## Overview

The application uses PostgreSQL as the database provider with Entity Framework Core 9.0 for data access and migrations. Migrations are automatically applied on application startup through the `DatabaseMigrationService`.

## Current Migrations

### 20251003033913_InitialCreate
- **Purpose**: Creates the initial database schema for all modules
- **Tables Created**:
  - `Users` - User accounts with email, password, and profile information
  - `Roles` - Role definitions with names and descriptions
  - `Permissions` - Permission definitions with resource-action-scope model
  - `UserRoles` - Many-to-many relationship between users and roles
  - `RolePermissions` - Many-to-many relationship between roles and permissions
  - `RefreshTokens` - JWT refresh tokens for authentication

- **Key Features**:
  - UUID v7 primary keys for better performance and natural ordering
  - PostgreSQL-specific optimizations (timestamptz, partial indexes)
  - Comprehensive indexing strategy for performance
  - Proper foreign key relationships where configured

## Automatic Migration

The application includes a `DatabaseMigrationService` that:

1. **Runs on startup** as a hosted service
2. **Checks database connectivity** before attempting migrations
3. **Applies pending migrations** automatically with timeout protection
4. **Validates schema** after migration completion
5. **Provides detailed logging** of the migration process

### Configuration

Migrations are configured in `InfrastructureModule.cs`:

```csharp
services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
});

services.AddHostedService<DatabaseMigrationService>();
```

## Manual Migration Commands

While migrations are applied automatically, you can also run them manually:

### Apply Migrations
```bash
# From src/Api directory
dotnet ef database update --project ../Infrastructure --startup-project .
```

### Create New Migration
```bash
# From src/Api directory
dotnet ef migrations add <MigrationName> --project ../Infrastructure --startup-project .
```

### List Migrations
```bash
# From src/Api directory
dotnet ef migrations list --project ../Infrastructure --startup-project .
```

### Remove Last Migration (if not applied)
```bash
# From src/Api directory
dotnet ef migrations remove --project ../Infrastructure --startup-project .
```

## Health Checks

The application includes database health checks accessible at:

- `/health` - Overall application health including database
- `/health/database` - Database-specific health check

The health check verifies:
- Database connectivity
- Migration status (applied vs pending)
- Basic schema validation

## Schema Validation

The `MigrationValidation` utility class provides:

- **Table existence checks** for all required tables
- **Index validation** for critical performance indexes
- **Migration status reporting** for monitoring
- **Schema integrity verification**

## Connection String

Configure the database connection in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ModularMonolith;Username=postgres;Password=postgres"
  }
}
```

## Troubleshooting

### Migration Fails on Startup
1. Check database connectivity
2. Verify PostgreSQL is running
3. Validate connection string
4. Check application logs for detailed error information

### Schema Validation Warnings
- Review the logs for specific missing tables or indexes
- Consider running manual migration commands
- Check entity configurations for consistency

### Performance Issues
- Monitor index usage with PostgreSQL query analysis
- Consider adding additional indexes for specific query patterns
- Review UUID v7 performance characteristics

## Development Workflow

1. **Make entity changes** in domain models
2. **Update entity configurations** if needed
3. **Create migration**: `dotnet ef migrations add <Name>`
4. **Review generated migration** for correctness
5. **Test migration** in development environment
6. **Commit migration files** to source control

## Production Considerations

- Migrations run automatically on startup
- Consider backup strategies before major schema changes
- Monitor migration performance and timeout settings
- Use blue-green deployments for zero-downtime updates
- Test migrations thoroughly in staging environments

## Files in This Directory

- `*.cs` - Migration implementation files
- `*.Designer.cs` - Migration metadata files
- `ApplicationDbContextModelSnapshot.cs` - Current model snapshot
- `MigrationValidation.cs` - Schema validation utilities
- `README.md` - This documentation file