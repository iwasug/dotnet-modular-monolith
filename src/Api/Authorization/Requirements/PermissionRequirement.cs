using Microsoft.AspNetCore.Authorization;

namespace ModularMonolith.Api.Authorization.Requirements;

/// <summary>
/// Authorization requirement for permission-based access control
/// </summary>
internal sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Resource { get; }
    public string Action { get; }
    public string Scope { get; }

    public PermissionRequirement(string resource, string action, string scope = "*")
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource cannot be null or empty", nameof(resource));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("Scope cannot be null or empty", nameof(scope));
        }

        Resource = resource.Trim().ToLowerInvariant();
        Action = action.Trim().ToLowerInvariant();
        Scope = scope.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Creates a permission requirement from a formatted string (resource:action:scope)
    /// </summary>
    public static PermissionRequirement FromString(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
        {
            throw new ArgumentException("Permission string cannot be null or empty", nameof(permissionString));
        }

        var parts = permissionString.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            2 => new PermissionRequirement(parts[0], parts[1]),
            3 => new PermissionRequirement(parts[0], parts[1], parts[2]),
            _ => throw new ArgumentException("Permission string must be in format 'resource:action' or 'resource:action:scope'", nameof(permissionString))
        };
    }

    /// <summary>
    /// Checks if this requirement matches a permission (supports wildcard matching)
    /// </summary>
    public bool Matches(string resource, string action, string scope)
    {
        return MatchesComponent(Resource, resource) &&
               MatchesComponent(Action, action) &&
               MatchesComponent(Scope, scope);
    }

    private static bool MatchesComponent(string requirementComponent, string permissionComponent)
    {
        return requirementComponent == "*" || 
               permissionComponent == "*" || 
               requirementComponent == permissionComponent;
    }

    public override string ToString() => $"{Resource}:{Action}:{Scope}";
}