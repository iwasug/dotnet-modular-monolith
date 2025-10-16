using Microsoft.EntityFrameworkCore;
using ModularMonolith.Users.Domain;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Shared.Domain;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Reflection;

namespace ModularMonolith.Infrastructure.Data;

/// <summary>
/// Base DbContext that automatically registers DbSet properties for all entities and applies configurations
/// </summary>
public abstract class DynamicDbContextBase(DbContextOptions options) : DbContext(options)
{
    private readonly Dictionary<Type, object> _dbSets = new();

    /// <summary>
    /// Gets a DbSet for the specified entity type, creating it dynamically if needed
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <returns>The DbSet for the entity type</returns>
    public new DbSet<TEntity> Set<TEntity>() where TEntity : class
    {
        var entityType = typeof(TEntity);
        
        if (_dbSets.TryGetValue(entityType, out var cachedDbSet))
        {
            return (DbSet<TEntity>)cachedDbSet;
        }

        var dbSet = base.Set<TEntity>();
        _dbSets[entityType] = dbSet;
        return dbSet;
    }

    /// <summary>
    /// Automatically discovers and registers all entity types from the specified assemblies
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="assemblies">Assemblies to scan for entities</param>
    protected void RegisterEntitiesFromAssemblies(ModelBuilder modelBuilder, params Assembly[] assemblies)
    {
        var entityTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t) || HasEntityConfiguration(t))
                .ToList();

            entityTypes.AddRange(types);
        }

        foreach (var entityType in entityTypes.Distinct())
        {
            // Register the entity type with EF Core
            modelBuilder.Entity(entityType);
        }
    }

    /// <summary>
    /// Automatically discovers and registers all entity types from module assemblies
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected void RegisterModuleEntities(ModelBuilder modelBuilder)
    {
        var moduleAssemblies = GetModuleAssemblies();
        RegisterEntitiesFromAssemblies(modelBuilder, moduleAssemblies.ToArray());
    }

    /// <summary>
    /// Applies all entity configurations from module assemblies automatically
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected void ApplyModuleConfigurations(ModelBuilder modelBuilder)
    {
        var moduleAssemblies = GetModuleAssemblies();
        
        foreach (var assembly in moduleAssemblies)
        {
            try
            {
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
            catch (Exception)
            {
                // Skip assemblies that can't be processed
                continue;
            }
        }
    }

    /// <summary>
    /// Applies global query filter for soft deletes to all BaseEntity types
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = System.Linq.Expressions.Expression.Lambda(System.Linq.Expressions.Expression.Not(property), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    /// <summary>
    /// Complete setup for dynamic DbContext with entity registration, configuration, and soft delete filter
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected void SetupDynamicDbContext(ModelBuilder modelBuilder)
    {
        // 1. Register all entities from modules
        RegisterModuleEntities(modelBuilder);
        
        // 2. Apply soft delete query filter
        ApplySoftDeleteQueryFilter(modelBuilder);
        
        // 3. Apply all configurations
        ApplyModuleConfigurations(modelBuilder);
    }

    /// <summary>
    /// Gets all assemblies that contain entities or configurations
    /// </summary>
    /// <returns>List of assemblies with entities or configurations</returns>
    private List<Assembly> GetModuleAssemblies()
    {
        var moduleAssemblies = new List<Assembly>();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null);

        foreach (var assembly in loadedAssemblies)
        {
            try
            {
                // Check if assembly contains BaseEntity types or IEntityTypeConfiguration implementations
                var hasEntities = assembly.GetTypes()
                    .Any(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t));

                var hasConfigurations = assembly.GetTypes()
                    .Any(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

                if (hasEntities || hasConfigurations)
                {
                    moduleAssemblies.Add(assembly);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
            catch (Exception)
            {
                // Skip assemblies that cause other exceptions
                continue;
            }
        }

        return moduleAssemblies;
    }

    /// <summary>
    /// Checks if a type has an entity configuration
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type has an entity configuration</returns>
    private bool HasEntityConfiguration(Type type)
    {
        var configurationTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>) &&
                         i.GetGenericArguments()[0] == type))
            .ToList();

        return configurationTypes.Any();
    }

    /// <summary>
    /// Gets all registered entity types
    /// </summary>
    /// <returns>Collection of registered entity types</returns>
    public IEnumerable<Type> GetRegisteredEntityTypes()
    {
        return Model.GetEntityTypes().Select(et => et.ClrType);
    }

    /// <summary>
    /// Gets a DbSet by entity type name
    /// </summary>
    /// <param name="entityTypeName">The entity type name</param>
    /// <returns>The DbSet as object, or null if not found</returns>
    public object? GetDbSetByName(string entityTypeName)
    {
        var entityType = GetRegisteredEntityTypes()
            .FirstOrDefault(t => t.Name.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase));

        if (entityType is null)
            return null;

        var method = typeof(DynamicDbContextBase).GetMethod(nameof(Set))!.MakeGenericMethod(entityType);
        return method.Invoke(this, null);
    }

    /// <summary>
    /// Gets all entity types that inherit from BaseEntity
    /// </summary>
    /// <returns>Collection of BaseEntity types</returns>
    public IEnumerable<Type> GetBaseEntityTypes()
    {
        return GetRegisteredEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t));
    }

    /// <summary>
    /// Gets all entity types from a specific module
    /// </summary>
    /// <param name="moduleNamespace">The module namespace (e.g., "ModularMonolith.Users")</param>
    /// <returns>Collection of entity types from the module</returns>
    public IEnumerable<Type> GetEntityTypesByModule(string moduleNamespace)
    {
        return GetRegisteredEntityTypes()
            .Where(t => t.Namespace?.StartsWith(moduleNamespace, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Checks if an entity type is registered in the context
    /// </summary>
    /// <param name="entityType">The entity type to check</param>
    /// <returns>True if the entity type is registered</returns>
    public bool IsEntityRegistered(Type entityType)
    {
        return GetRegisteredEntityTypes().Contains(entityType);
    }

    /// <summary>
    /// Checks if an entity type is registered in the context
    /// </summary>
    /// <typeparam name="TEntity">The entity type to check</typeparam>
    /// <returns>True if the entity type is registered</returns>
    public bool IsEntityRegistered<TEntity>() where TEntity : class
    {
        return IsEntityRegistered(typeof(TEntity));
    }

    /// <summary>
    /// Gets statistics about registered entities
    /// </summary>
    /// <returns>Entity registration statistics</returns>
    public EntityRegistrationStats GetEntityStats()
    {
        var allEntities = GetRegisteredEntityTypes().ToList();
        var baseEntities = GetBaseEntityTypes().ToList();
        
        var moduleStats = new Dictionary<string, int>();
        
        foreach (var entity in allEntities)
        {
            var namespaceParts = entity.Namespace?.Split('.') ?? Array.Empty<string>();
            if (namespaceParts.Length >= 2)
            {
                var moduleName = string.Join(".", namespaceParts.Take(2));
                moduleStats[moduleName] = moduleStats.GetValueOrDefault(moduleName, 0) + 1;
            }
        }

        return new EntityRegistrationStats
        {
            TotalEntities = allEntities.Count,
            BaseEntities = baseEntities.Count,
            ModuleStats = moduleStats
        };
    }

    /// <summary>
    /// Discovers all entity types from the specified assemblies
    /// </summary>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Collection of discovered entity types</returns>
    public static IEnumerable<Type> DiscoverEntityTypes(params Assembly[] assemblies)
    {
        var entityTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => typeof(BaseEntity).IsAssignableFrom(t))
                    .ToList();

                entityTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
        }

        return entityTypes.Distinct();
    }

    /// <summary>
    /// Discovers all entity types from all loaded assemblies that contain BaseEntity types
    /// </summary>
    /// <returns>Collection of discovered entity types</returns>
    public static IEnumerable<Type> DiscoverModuleEntityTypes()
    {
        return DiscoverModuleEntityTypes(null);
    }

    /// <summary>
    /// Discovers all entity types from assemblies that match the specified namespace pattern
    /// </summary>
    /// <param name="namespacePattern">Optional namespace pattern to filter assemblies (e.g., "MyApp" to include MyApp.*)</param>
    /// <returns>Collection of discovered entity types</returns>
    public static IEnumerable<Type> DiscoverModuleEntityTypes(string? namespacePattern)
    {
        var relevantAssemblies = new List<Assembly>();
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null);

        foreach (var assembly in loadedAssemblies)
        {
            try
            {
                // Apply namespace pattern filter if specified
                if (!string.IsNullOrEmpty(namespacePattern))
                {
                    var assemblyName = assembly.FullName!;
                    if (!assemblyName.Contains(namespacePattern, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // Check if assembly contains BaseEntity types
                var hasEntities = assembly.GetTypes()
                    .Any(t => t.IsClass && !t.IsAbstract && typeof(BaseEntity).IsAssignableFrom(t));

                if (hasEntities)
                {
                    relevantAssemblies.Add(assembly);
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded
                continue;
            }
            catch (Exception)
            {
                // Skip assemblies that cause other exceptions
                continue;
            }
        }

        return DiscoverEntityTypes(relevantAssemblies.ToArray());
    }
}

/// <summary>
/// Statistics about entity registration in the context
/// </summary>
public class EntityRegistrationStats
{
    public int TotalEntities { get; set; }
    public int BaseEntities { get; set; }
    public Dictionary<string, int> ModuleStats { get; set; } = new();

    public override string ToString()
    {
        var moduleInfo = string.Join(", ", ModuleStats.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        return $"Total: {TotalEntities}, BaseEntities: {BaseEntities}, Modules: [{moduleInfo}]";
    }
}

/// <summary>
/// Application database context for the modular monolith with automatic audit functionality and dynamic entity registration
/// </summary>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor httpContextAccessor)
    : DynamicDbContextBase(options)
{
    // Dynamic DbSet properties - automatically available for all registered entities
    // Access any entity using: Set<EntityType>() or context.Set<User>(), context.Set<Role>(), etc.
    
    // Optional: Expose commonly used entities as properties for convenience
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ModularMonolith.Shared.Domain.Permission> Permissions => Set<ModularMonolith.Shared.Domain.Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Complete setup for dynamic DbContext - one line does everything!
        SetupDynamicDbContext(modelBuilder);
        
        // Seed initial data - temporarily disabled due to value object complexity
        // Use DataSeeder.SeedAsync() after migrations instead
        // Configurations.SeedDataConfiguration.SeedData(modelBuilder);
        
        // In development, you can enable detailed logging by uncommenting the line below:
        // ApplyConfigurationsWithLogging(modelBuilder, message => System.Diagnostics.Debug.WriteLine($"[EF Configuration] {message}"));
    }

    /// <summary>
    /// Applies configurations with detailed logging for debugging (development only)
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="logAction">Action to log messages</param>
    private void ApplyConfigurationsWithLogging(ModelBuilder modelBuilder, Action<string> logAction)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null)
            .Where(a => a.FullName!.StartsWith("ModularMonolith", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        logAction($"Found {assemblies.Length} ModularMonolith assemblies to scan for configurations");

        foreach (var assembly in assemblies)
        {
            try
            {
                var configurationTypes = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
                    .ToList();

                logAction($"Assembly {assembly.GetName().Name}: Found {configurationTypes.Count} configuration types");

                foreach (var configurationType in configurationTypes)
                {
                    logAction($"  - {configurationType.Name}");
                }

                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                logAction($"Error processing assembly {assembly.GetName().Name}: {ex.Message}");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var currentUserId = GetCurrentUserId();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(currentUserId ?? Guid.Empty);
                    break;

                case EntityState.Modified:
                    // Only update if not already set (to avoid overriding explicit audit info)
                    var currentUpdatedBy = entry.Property(nameof(BaseEntity.UpdatedBy)).CurrentValue;
                    if (currentUpdatedBy is null || currentUpdatedBy.Equals(Guid.Empty))
                    {
                        entry.Entity.UpdateTimestamp(currentUserId);
                    }
                    break;
            }
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        return userId;
    }
}