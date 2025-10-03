using ModularMonolith.Shared.Domain;
using System.Reflection;

namespace ModularMonolith.Shared.Authorization;

/// <summary>
/// Central registry for all module permissions
/// </summary>
public sealed class PermissionRegistry
{
    private readonly Dictionary<string, IModulePermissions> _modulePermissions = new();
    private readonly Lazy<IReadOnlyList<Permission>> _allPermissions;

    public PermissionRegistry()
    {
        _allPermissions = new Lazy<IReadOnlyList<Permission>>(LoadAllPermissions);
        DiscoverAndRegisterModulePermissions();
    }

    /// <summary>
    /// Registers a module's permissions
    /// </summary>
    /// <param name="modulePermissions">Module permissions implementation</param>
    public void RegisterModule(IModulePermissions modulePermissions)
    {
        if (modulePermissions is null)
            throw new ArgumentNullException(nameof(modulePermissions));

        _modulePermissions[modulePermissions.ModuleName] = modulePermissions;
    }

    /// <summary>
    /// Gets all permissions from all registered modules
    /// </summary>
    /// <returns>Complete list of all permissions</returns>
    public IReadOnlyList<Permission> GetAllPermissions()
    {
        return _allPermissions.Value;
    }

    /// <summary>
    /// Gets permissions for a specific module
    /// </summary>
    /// <param name="moduleName">Name of the module</param>
    /// <returns>Permissions for the specified module</returns>
    public IReadOnlyList<Permission> GetModulePermissions(string moduleName)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            throw new ArgumentException("Module name cannot be null or empty", nameof(moduleName));

        return _modulePermissions.TryGetValue(moduleName, out var modulePermissions)
            ? modulePermissions.GetPermissions()
            : Array.Empty<Permission>();
    }

    /// <summary>
    /// Gets all registered module names
    /// </summary>
    /// <returns>List of registered module names</returns>
    public IReadOnlyList<string> GetRegisteredModules()
    {
        return _modulePermissions.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets permissions grouped by module
    /// </summary>
    /// <returns>Dictionary of module name to permissions</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<Permission>> GetPermissionsByModule()
    {
        return _modulePermissions.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetPermissions()
        ).AsReadOnly();
    }

    /// <summary>
    /// Gets permissions grouped by resource
    /// </summary>
    /// <returns>Dictionary of resource to permissions</returns>
    public IReadOnlyDictionary<string, IReadOnlyList<Permission>> GetPermissionsByResource()
    {
        return GetAllPermissions()
            .GroupBy(p => p.Resource)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Permission>)g.ToList().AsReadOnly()
            ).AsReadOnly();
    }

    /// <summary>
    /// Finds permissions by resource and action
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <returns>Matching permissions</returns>
    public IReadOnlyList<Permission> FindPermissions(string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be null or empty", nameof(resource));
        
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        return GetAllPermissions()
            .Where(p => p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase) &&
                       p.Action.Equals(action, StringComparison.OrdinalIgnoreCase))
            .ToList().AsReadOnly();
    }

    /// <summary>
    /// Finds a specific permission by resource, action, and scope
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="scope">Scope name</param>
    /// <returns>Matching permission or null if not found</returns>
    public Permission? FindPermission(string resource, string action, string scope = "*")
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be null or empty", nameof(resource));
        
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        scope ??= "*";

        return GetAllPermissions()
            .FirstOrDefault(p => 
                p.Resource.Equals(resource, StringComparison.OrdinalIgnoreCase) &&
                p.Action.Equals(action, StringComparison.OrdinalIgnoreCase) &&
                p.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a permission exists
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="scope">Scope name</param>
    /// <returns>True if permission exists</returns>
    public bool HasPermission(string resource, string action, string scope = "*")
    {
        return FindPermission(resource, action, scope) is not null;
    }

    /// <summary>
    /// Gets statistics about registered permissions
    /// </summary>
    /// <returns>Permission statistics</returns>
    public PermissionStatistics GetStatistics()
    {
        var allPermissions = GetAllPermissions();
        var permissionsByModule = GetPermissionsByModule();
        var permissionsByResource = GetPermissionsByResource();

        return new PermissionStatistics
        {
            TotalPermissions = allPermissions.Count,
            TotalModules = _modulePermissions.Count,
            TotalResources = permissionsByResource.Count,
            ModulePermissionCounts = permissionsByModule.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            ),
            ResourcePermissionCounts = permissionsByResource.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count
            )
        };
    }

    private IReadOnlyList<Permission> LoadAllPermissions()
    {
        var allPermissions = new List<Permission>();

        foreach (var modulePermissions in _modulePermissions.Values)
        {
            allPermissions.AddRange(modulePermissions.GetPermissions());
        }

        // Remove duplicates based on Resource, Action, and Scope
        return allPermissions
            .GroupBy(p => new { p.Resource, p.Action, p.Scope })
            .Select(g => g.First())
            .ToList().AsReadOnly();
    }

    private void DiscoverAndRegisterModulePermissions()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.FullName is not null)
            .Where(a => a.FullName!.StartsWith("ModularMonolith", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assembly in assemblies)
        {
            try
            {
                var modulePermissionTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => typeof(IModulePermissions).IsAssignableFrom(t))
                    .ToList();

                foreach (var type in modulePermissionTypes)
                {
                    if (Activator.CreateInstance(type) is IModulePermissions modulePermissions)
                    {
                        RegisterModule(modulePermissions);
                    }
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
    }
}

/// <summary>
/// Statistics about registered permissions
/// </summary>
public sealed class PermissionStatistics
{
    public int TotalPermissions { get; init; }
    public int TotalModules { get; init; }
    public int TotalResources { get; init; }
    public Dictionary<string, int> ModulePermissionCounts { get; init; } = new();
    public Dictionary<string, int> ResourcePermissionCounts { get; init; } = new();

    public override string ToString()
    {
        var moduleInfo = string.Join(", ", ModulePermissionCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        var resourceInfo = string.Join(", ", ResourcePermissionCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        
        return $"Total: {TotalPermissions} permissions, {TotalModules} modules, {TotalResources} resources\n" +
               $"Modules: [{moduleInfo}]\n" +
               $"Resources: [{resourceInfo}]";
    }
}