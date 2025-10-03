using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ModularMonolith.Api.Authorization.Requirements;

namespace ModularMonolith.Api.Authorization.Policies;

/// <summary>
/// Dynamic authorization policy provider that creates policies on-demand for permissions and roles
/// </summary>
internal sealed class DynamicAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle permission policies
        if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(CreatePermissionPolicy(policyName));
        }

        // Handle role policies
        if (policyName.StartsWith("Role:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(CreateRolePolicy(policyName));
        }

        // Fall back to default policy provider for other policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }

    private static AuthorizationPolicy? CreatePermissionPolicy(string policyName)
    {
        try
        {
            // Extract permission from policy name: "Permission:resource:action:scope"
            var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || parts.Length > 4)
            {
                return null;
            }

            var resource = parts[1];
            var action = parts[2];
            var scope = parts.Length == 4 ? parts[3] : "*";

            var requirement = new PermissionRequirement(resource, action, scope);
            
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();
        }
        catch
        {
            return null;
        }
    }

    private static AuthorizationPolicy? CreateRolePolicy(string policyName)
    {
        try
        {
            // Extract role information from policy name: "Role:Any:role1,role2" or "Role:All:role1,role2"
            var parts = policyName.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return null;
            }

            var conjunction = parts[1]; // "Any" or "All"
            var roleList = parts[2]; // "role1,role2"
            
            var roles = roleList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(r => r.Trim())
                               .Where(r => !string.IsNullOrEmpty(r))
                               .ToArray();

            if (roles.Length == 0)
            {
                return null;
            }

            var requireAllRoles = string.Equals(conjunction, "All", StringComparison.OrdinalIgnoreCase);
            var requirement = new RoleRequirement(roles, requireAllRoles);

            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();
        }
        catch
        {
            return null;
        }
    }
}