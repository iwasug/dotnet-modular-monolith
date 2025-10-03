using ModularMonolith.Shared.Authorization;
using ModularMonolith.Users.Authorization;
using ModularMonolith.Roles.Authorization;
using ModularMonolith.Authentication.Authorization;
using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Api.Examples;

/// <summary>
/// Examples of how to use permission constants and registry
/// </summary>
public static class PermissionExamples
{
    /// <summary>
    /// Example: Creating predefined role permissions
    /// </summary>
    public static List<Permission> CreateAdminRolePermissions()
    {
        return new List<Permission>
        {
            // User permissions
            UserPermissions.ReadAll,
            UserPermissions.WriteAll,
            UserPermissions.CreateAll,
            UserPermissions.UpdateAll,
            UserPermissions.DeleteAll,
            UserPermissions.ManageAll,

            // Role permissions
            RolePermissions.ReadAll,
            RolePermissions.WriteAll,
            RolePermissions.CreateAll,
            RolePermissions.UpdateAll,
            RolePermissions.DeleteAll,
            RolePermissions.AssignAll,
            RolePermissions.RevokeAll,
            RolePermissions.ManageAll,

            // Authentication permissions
            AuthenticationPermissions.AuthAdmin,
            AuthenticationPermissions.TokenAdmin,
            AuthenticationPermissions.SessionAdmin
        };
    }

    /// <summary>
    /// Example: Creating manager role permissions (department level)
    /// </summary>
    public static List<Permission> CreateManagerRolePermissions()
    {
        return new List<Permission>
        {
            // User permissions - department level
            UserPermissions.ReadDepartment,
            UserPermissions.WriteDepartment,
            UserPermissions.CreateDepartment,
            UserPermissions.UpdateDepartment,
            UserPermissions.ManageDepartment,

            // Role permissions - department level
            RolePermissions.ReadDepartment,
            RolePermissions.WriteDepartment,
            RolePermissions.AssignDepartment,
            RolePermissions.RevokeDepartment,

            // Authentication permissions - department level
            AuthenticationPermissions.RevokeTokenDepartment,
            AuthenticationPermissions.ReadSessionDepartment,
            AuthenticationPermissions.ManageSessionDepartment
        };
    }

    /// <summary>
    /// Example: Creating basic user permissions
    /// </summary>
    public static List<Permission> CreateBasicUserPermissions()
    {
        return new List<Permission>
        {
            // Self permissions only
            UserPermissions.ReadSelf,
            UserPermissions.WriteSelf,
            UserPermissions.UpdateSelf,

            // Basic authentication permissions
            AuthenticationPermissions.LoginAll,
            AuthenticationPermissions.LogoutSelf,
            AuthenticationPermissions.RefreshTokenSelf,
            AuthenticationPermissions.ReadSessionSelf,

            // Read organization-level roles (to see available roles)
            RolePermissions.ReadOrganization
        };
    }

    /// <summary>
    /// Example: Using permission registry to find permissions
    /// </summary>
    public static void ExampleUsingPermissionRegistry(PermissionRegistry registry)
    {
        // Get all permissions
        var allPermissions = registry.GetAllPermissions();
        Console.WriteLine($"Total permissions: {allPermissions.Count}");

        // Get permissions by module
        var userPermissions = registry.GetModulePermissions("Users");
        Console.WriteLine($"User module permissions: {userPermissions.Count}");

        // Find specific permission
        var userReadPermission = registry.FindPermission("user", "read", "*");
        if (userReadPermission is not null)
        {
            Console.WriteLine($"Found permission: {userReadPermission.Resource}:{userReadPermission.Action}:{userReadPermission.Scope}");
        }

        // Search permissions
        var readPermissions = registry.FindPermissions("user", "read");
        Console.WriteLine($"User read permissions: {readPermissions.Count}");

        // Get statistics
        var stats = registry.GetStatistics();
        Console.WriteLine($"Permission statistics: {stats}");
    }

    /// <summary>
    /// Example: Creating custom permissions using constants
    /// </summary>
    public static List<Permission> CreateCustomPermissions()
    {
        return new List<Permission>
        {
            // Using constants for consistency
            Permission.Create(UserPermissions.RESOURCE, UserPermissions.Actions.READ, "team"),
            Permission.Create(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE, "team"),
            Permission.Create(RolePermissions.RESOURCE, RolePermissions.Actions.ASSIGN, "team"),
            
            // Custom resource with standard actions
            Permission.Create("report", PermissionConstants.READ, PermissionConstants.ALL),
            Permission.Create("report", PermissionConstants.CREATE, PermissionConstants.DEPARTMENT),
            Permission.Create("report", PermissionConstants.DELETE, PermissionConstants.SELF),
            
            // Custom actions
            Permission.Create("audit", "view", PermissionConstants.ALL),
            Permission.Create("audit", "export", PermissionConstants.DEPARTMENT),
            Permission.Create("backup", "create", PermissionConstants.ALL),
            Permission.Create("backup", "restore", PermissionConstants.ALL)
        };
    }

    /// <summary>
    /// Example: Permission validation helper
    /// </summary>
    public static bool ValidateUserPermissions(List<Permission> userPermissions, string requiredResource, string requiredAction, string requiredScope = "*")
    {
        // Check exact match
        var exactMatch = userPermissions.Any(p => 
            p.Resource.Equals(requiredResource, StringComparison.OrdinalIgnoreCase) &&
            p.Action.Equals(requiredAction, StringComparison.OrdinalIgnoreCase) &&
            p.Scope.Equals(requiredScope, StringComparison.OrdinalIgnoreCase));

        if (exactMatch) return true;

        // Check wildcard matches
        var wildcardMatch = userPermissions.Any(p =>
            (p.Resource == "*" || p.Resource.Equals(requiredResource, StringComparison.OrdinalIgnoreCase)) &&
            (p.Action == "*" || p.Action.Equals(requiredAction, StringComparison.OrdinalIgnoreCase)) &&
            (p.Scope == "*" || p.Scope.Equals(requiredScope, StringComparison.OrdinalIgnoreCase)));

        return wildcardMatch;
    }

    /// <summary>
    /// Example: Getting permission summary for a role
    /// </summary>
    public static object GetPermissionSummary(List<Permission> permissions)
    {
        var groupedByResource = permissions
            .GroupBy(p => p.Resource)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(p => p.Action)
                      .ToDictionary(
                          ag => ag.Key,
                          ag => ag.Select(p => p.Scope).ToList()
                      )
            );

        return new
        {
            TotalPermissions = permissions.Count,
            Resources = groupedByResource.Keys.ToList(),
            ResourceCount = groupedByResource.Count,
            DetailedBreakdown = groupedByResource,
            Summary = string.Join(", ", groupedByResource.Select(kvp => 
                $"{kvp.Key}: {kvp.Value.Sum(actions => actions.Value.Count)} permissions"))
        };
    }
}