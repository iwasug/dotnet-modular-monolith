using Microsoft.AspNetCore.Authorization;

namespace ModularMonolith.Api.Authorization.Attributes;

/// <summary>
/// Attribute for requiring specific permissions on controllers or actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public string Resource { get; }
    public string Action { get; }
    public string Scope { get; }

    public RequirePermissionAttribute(string resource, string action, string scope = "*")
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

        // Set the policy name to be used by the authorization system
        Policy = $"Permission:{Resource}:{Action}:{Scope}";
    }

    /// <summary>
    /// Creates a permission attribute from a formatted string (resource:action:scope)
    /// </summary>
    public static RequirePermissionAttribute FromString(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
        {
            throw new ArgumentException("Permission string cannot be null or empty", nameof(permissionString));
        }

        string[] parts = permissionString.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            2 => new RequirePermissionAttribute(parts[0], parts[1]),
            3 => new RequirePermissionAttribute(parts[0], parts[1], parts[2]),
            _ => throw new ArgumentException("Permission string must be in format 'resource:action' or 'resource:action:scope'", nameof(permissionString))
        };
    }

    public override string ToString() => $"{Resource}:{Action}:{Scope}";
}