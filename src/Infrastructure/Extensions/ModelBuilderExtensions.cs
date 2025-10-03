using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ModularMonolith.Infrastructure.Extensions;

/// <summary>
/// Extension methods for ModelBuilder to automatically apply configurations
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Automatically applies all IEntityTypeConfiguration implementations from the specified assemblies
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="assemblies">Assemblies to scan for configurations</param>
    public static void ApplyConfigurationsFromAssemblies(this ModelBuilder modelBuilder, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }
    }

    /// <summary>
    /// Automatically applies all IEntityTypeConfiguration implementations from the current application domain
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ApplyAllConfigurations(this ModelBuilder modelBuilder)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null)
            .Where(a => a.FullName!.StartsWith("ModularMonolith", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assembly in assemblies)
        {
            try
            {
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
            catch (Exception)
            {
                // Skip assemblies that can't be processed
                // This prevents issues with assemblies that don't contain configurations
                continue;
            }
        }
    }

    /// <summary>
    /// Applies configurations from specific module assemblies
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    public static void ApplyModuleConfigurations(this ModelBuilder modelBuilder)
    {
        var moduleAssemblies = new List<Assembly>();

        // Get all loaded assemblies that contain our modules
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null);

        foreach (var assembly in loadedAssemblies)
        {
            var assemblyName = assembly.FullName;
            
            // Check if this is one of our module assemblies
            if (assemblyName!.Contains("ModularMonolith.Users") ||
                assemblyName.Contains("ModularMonolith.Roles") ||
                assemblyName.Contains("ModularMonolith.Authentication") ||
                assemblyName.Contains("ModularMonolith.Infrastructure"))
            {
                moduleAssemblies.Add(assembly);
            }
        }

        // Apply configurations from each module assembly
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
    /// Applies configurations with detailed logging for debugging
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    /// <param name="logAction">Optional action to log configuration details</param>
    public static void ApplyConfigurationsWithLogging(this ModelBuilder modelBuilder, Action<string>? logAction = null)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null)
            .Where(a => a.FullName!.StartsWith("ModularMonolith", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        logAction?.Invoke($"Found {assemblies.Length} ModularMonolith assemblies to scan for configurations");

        foreach (var assembly in assemblies)
        {
            try
            {
                var configurationTypes = assembly.GetTypes()
                    .Where(t => t.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
                    .ToList();

                logAction?.Invoke($"Assembly {assembly.GetName().Name}: Found {configurationTypes.Count} configuration types");

                foreach (var configurationType in configurationTypes)
                {
                    logAction?.Invoke($"  - {configurationType.Name}");
                }

                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"Error processing assembly {assembly.GetName().Name}: {ex.Message}");
            }
        }
    }
}