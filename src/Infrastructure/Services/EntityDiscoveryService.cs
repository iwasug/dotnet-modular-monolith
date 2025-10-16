using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Infrastructure.Services;

/// <summary>
/// Service for discovering and managing entities dynamically
/// </summary>
public interface IEntityDiscoveryService
{
    /// <summary>
    /// Discovers all entity types from module assemblies
    /// </summary>
    /// <returns>Collection of discovered entity types</returns>
    IEnumerable<Type> DiscoverEntityTypes();

    /// <summary>
    /// Gets entity registration statistics
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>Entity registration statistics</returns>
    EntityRegistrationStats GetEntityStats(DynamicDbContextBase context);

    /// <summary>
    /// Validates that all discovered entities are properly registered
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>Validation results</returns>
    EntityValidationResult ValidateEntityRegistration(DynamicDbContextBase context);

    /// <summary>
    /// Gets detailed information about registered entities
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>Detailed entity information</returns>
    IEnumerable<EntityInfo> GetEntityDetails(DynamicDbContextBase context);
}

/// <summary>
/// Implementation of entity discovery service
/// </summary>
internal sealed class EntityDiscoveryService(ILogger<EntityDiscoveryService> logger) : IEntityDiscoveryService
{
    public IEnumerable<Type> DiscoverEntityTypes()
    {
        logger.LogDebug("Discovering entity types from module assemblies");

        var entityTypes = DynamicDbContextBase.DiscoverModuleEntityTypes().ToList();

        logger.LogInformation("Discovered {Count} entity types", entityTypes.Count);

        return entityTypes;
    }

    public EntityRegistrationStats GetEntityStats(DynamicDbContextBase context)
    {
        logger.LogDebug("Getting entity registration statistics");

        var stats = context.GetEntityStats();

        logger.LogInformation("Entity stats: {Stats}", stats);

        return stats;
    }

    public EntityValidationResult ValidateEntityRegistration(DynamicDbContextBase context)
    {
        logger.LogDebug("Validating entity registration");

        var discoveredTypes = DiscoverEntityTypes().ToList();
        var registeredTypes = context.GetRegisteredEntityTypes().ToList();

        var missingTypes = discoveredTypes.Except(registeredTypes).ToList();
        var extraTypes = registeredTypes.Except(discoveredTypes)
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t))
            .ToList();

        var result = new EntityValidationResult
        {
            DiscoveredCount = discoveredTypes.Count,
            RegisteredCount = registeredTypes.Count,
            MissingTypes = missingTypes,
            ExtraTypes = extraTypes,
            IsValid = !missingTypes.Any()
        };

        if (!result.IsValid)
        {
            logger.LogWarning("Entity validation failed. Missing types: {MissingTypes}", 
                string.Join(", ", missingTypes.Select(t => t.Name)));
        }
        else
        {
            logger.LogInformation("Entity validation passed. All {Count} discovered entities are registered", 
                discoveredTypes.Count);
        }

        return result;
    }

    public IEnumerable<EntityInfo> GetEntityDetails(DynamicDbContextBase context)
    {
        logger.LogDebug("Getting detailed entity information");

        var entityInfos = new List<EntityInfo>();

        foreach (var entityType in context.GetRegisteredEntityTypes())
        {
            var entityInfo = new EntityInfo
            {
                Type = entityType,
                Name = entityType.Name,
                FullName = entityType.FullName ?? entityType.Name,
                Namespace = entityType.Namespace ?? "Unknown",
                IsBaseEntity = typeof(BaseEntity).IsAssignableFrom(entityType),
                Assembly = entityType.Assembly.GetName().Name ?? "Unknown",
                Module = GetModuleName(entityType)
            };

            entityInfos.Add(entityInfo);
        }

        logger.LogDebug("Retrieved details for {Count} entities", entityInfos.Count);

        return entityInfos.OrderBy(e => e.Module).ThenBy(e => e.Name);
    }

    private static string GetModuleName(Type entityType)
    {
        var namespaceParts = entityType.Namespace?.Split('.') ?? Array.Empty<string>();
        
        if (namespaceParts.Length >= 2)
        {
            return string.Join(".", namespaceParts.Take(2));
        }

        return "Unknown";
    }
}

/// <summary>
/// Result of entity validation
/// </summary>
public class EntityValidationResult
{
    public int DiscoveredCount { get; set; }
    public int RegisteredCount { get; set; }
    public List<Type> MissingTypes { get; set; } = new();
    public List<Type> ExtraTypes { get; set; } = new();
    public bool IsValid { get; set; }

    public override string ToString()
    {
        return $"Discovered: {DiscoveredCount}, Registered: {RegisteredCount}, Valid: {IsValid}";
    }
}

/// <summary>
/// Detailed information about an entity
/// </summary>
public class EntityInfo
{
    public Type Type { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsBaseEntity { get; set; }
    public string Assembly { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Module}.{Name} ({Assembly})";
    }
}