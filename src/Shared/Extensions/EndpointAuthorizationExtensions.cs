using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using ModularMonolith.Shared.Authorization;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for adding authorization to minimal API endpoints
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>
    /// Requires a specific permission for the endpoint
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string resource, string action, string scope = "*")
        where TBuilder : IEndpointConventionBuilder
    {
        var policyName = $"Permission:{resource.ToLowerInvariant()}:{action.ToLowerInvariant()}:{scope.ToLowerInvariant()}";
        return builder.RequireAuthorization(policyName);
    }

    /// <summary>
    /// Requires a specific permission for the endpoint using a formatted string
    /// </summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permissionString)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(permissionString))
        {
            throw new ArgumentException("Permission string cannot be null or empty", nameof(permissionString));
        }

        var parts = permissionString.Split(':', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            2 => builder.RequirePermission(parts[0], parts[1]),
            3 => builder.RequirePermission(parts[0], parts[1], parts[2]),
            _ => throw new ArgumentException("Permission string must be in format 'resource:action' or 'resource:action:scope'", nameof(permissionString))
        };
    }

    /// <summary>
    /// Requires any of the specified roles for the endpoint
    /// </summary>
    public static TBuilder RequireAnyRole<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        if (roles is null || roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified", nameof(roles));
        }

        var normalizedRoles = roles.Select(r => r.ToLowerInvariant()).ToArray();
        var policyName = $"Role:Any:{string.Join(",", normalizedRoles)}";
        return builder.RequireAuthorization(policyName);
    }

    /// <summary>
    /// Requires all of the specified roles for the endpoint
    /// </summary>
    public static TBuilder RequireAllRoles<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        if (roles is null || roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified", nameof(roles));
        }

        var normalizedRoles = roles.Select(r => r.ToLowerInvariant()).ToArray();
        var policyName = $"Role:All:{string.Join(",", normalizedRoles)}";
        return builder.RequireAuthorization(policyName);
    }

    /// <summary>
    /// Requires authentication for the endpoint
    /// </summary>
    public static TBuilder RequireAuthentication<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization();
    }

}

/// <summary>
/// Common permission shortcuts
/// </summary>
public static class PermissionExtensions
{
    // User permissions
    public static TBuilder RequireUserRead<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("user", "read");

    public static TBuilder RequireUserWrite<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("user", "write");

    public static TBuilder RequireUserDelete<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("user", "delete");

    public static TBuilder RequireUserManage<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("user", "manage");

    // Role permissions
    public static TBuilder RequireRoleRead<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("role", "read");

    public static TBuilder RequireRoleWrite<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("role", "write");

    public static TBuilder RequireRoleDelete<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("role", "delete");

    public static TBuilder RequireRoleManage<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("role", "manage");

    public static TBuilder RequireRoleAssign<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("role", "assign");

    // System permissions
    public static TBuilder RequireSystemAdmin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission("*", "*");
}