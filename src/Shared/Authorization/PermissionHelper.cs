using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Shared.Authorization;

/// <summary>
/// Helper class for working with permissions
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Creates a permission from string components
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <param name="action">Action name</param>
    /// <param name="scope">Scope name (defaults to "*")</param>
    /// <returns>Permission instance</returns>
    public static Permission CreatePermission(string resource, string action, string scope = "*")
    {
        return Permission.Create(resource, action, scope);
    }

    /// <summary>
    /// Creates a permission from a formatted string (resource:action:scope)
    /// </summary>
    /// <param name="permissionString">Permission string in format "resource:action" or "resource:action:scope"</param>
    /// <returns>Permission instance</returns>
    public static Permission CreatePermissionFromString(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
            throw new ArgumentException("Permission string cannot be null or empty", nameof(permissionString));

        var parts = permissionString.Split(':');
        
        return parts.Length switch
        {
            2 => CreatePermission(parts[0], parts[1]),
            3 => CreatePermission(parts[0], parts[1], parts[2]),
            _ => throw new ArgumentException("Permission string must be in format 'resource:action' or 'resource:action:scope'", nameof(permissionString))
        };
    }

    /// <summary>
    /// Formats a permission as a string
    /// </summary>
    /// <param name="permission">Permission to format</param>
    /// <returns>Formatted permission string</returns>
    public static string FormatPermission(Permission permission)
    {
        if (permission is null)
            throw new ArgumentNullException(nameof(permission));

        return $"{permission.Resource}:{permission.Action}:{permission.Scope}";
    }

    /// <summary>
    /// Checks if two permissions are equivalent
    /// </summary>
    /// <param name="permission1">First permission</param>
    /// <param name="permission2">Second permission</param>
    /// <returns>True if permissions are equivalent</returns>
    public static bool AreEquivalent(Permission permission1, Permission permission2)
    {
        if (permission1 is null || permission2 is null)
            return false;

        return permission1.Resource.Equals(permission2.Resource, StringComparison.OrdinalIgnoreCase) &&
               permission1.Action.Equals(permission2.Action, StringComparison.OrdinalIgnoreCase) &&
               permission1.Scope.Equals(permission2.Scope, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a permission matches a pattern (supports wildcards)
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <param name="resourcePattern">Resource pattern (supports "*" wildcard)</param>
    /// <param name="actionPattern">Action pattern (supports "*" wildcard)</param>
    /// <param name="scopePattern">Scope pattern (supports "*" wildcard)</param>
    /// <returns>True if permission matches the pattern</returns>
    public static bool MatchesPattern(Permission permission, string resourcePattern, string actionPattern, string scopePattern = "*")
    {
        if (permission is null)
            return false;

        return MatchesWildcard(permission.Resource, resourcePattern) &&
               MatchesWildcard(permission.Action, actionPattern) &&
               MatchesWildcard(permission.Scope, scopePattern);
    }

    /// <summary>
    /// Gets all permissions that match a specific pattern
    /// </summary>
    /// <param name="permissions">List of permissions to filter</param>
    /// <param name="resourcePattern">Resource pattern</param>
    /// <param name="actionPattern">Action pattern</param>
    /// <param name="scopePattern">Scope pattern</param>
    /// <returns>Filtered list of permissions</returns>
    public static IReadOnlyList<Permission> FilterByPattern(
        IEnumerable<Permission> permissions, 
        string resourcePattern, 
        string actionPattern, 
        string scopePattern = "*")
    {
        return permissions
            .Where(p => MatchesPattern(p, resourcePattern, actionPattern, scopePattern))
            .ToList()
            .AsReadOnly();
    }

    private static bool MatchesWildcard(string value, string pattern)
    {
        if (pattern == "*")
            return true;

        return value.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}