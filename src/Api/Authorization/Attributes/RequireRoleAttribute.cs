using Microsoft.AspNetCore.Authorization;

namespace ModularMonolith.Api.Authorization.Attributes;

/// <summary>
/// Attribute for requiring specific roles on controllers or actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RequireRoleAttribute : AuthorizeAttribute
{
    public IReadOnlyList<string> RequiredRoles { get; }
    public bool RequireAllRoles { get; }

    public RequireRoleAttribute(params string[] requiredRoles) : this(false, requiredRoles)
    {
    }

    public RequireRoleAttribute(bool requireAllRoles, params string[] requiredRoles)
    {
        if (requiredRoles is null || requiredRoles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified", nameof(requiredRoles));
        }

        List<string> roles = requiredRoles.Where(r => !string.IsNullOrWhiteSpace(r))
                                .Select(r => r.Trim().ToLowerInvariant())
                                .Distinct()
                                .ToList();

        if (roles.Count == 0)
        {
            throw new ArgumentException("At least one valid role must be specified", nameof(requiredRoles));
        }

        RequiredRoles = roles.AsReadOnly();
        RequireAllRoles = requireAllRoles;

        // Set the policy name to be used by the authorization system
        string conjunction = RequireAllRoles ? "All" : "Any";
        string roleList = string.Join(",", RequiredRoles);
        Policy = $"Role:{conjunction}:{roleList}";
    }

    /// <summary>
    /// Creates a role attribute that requires ALL specified roles
    /// </summary>
    public static RequireRoleAttribute RequireAll(params string[] roles)
    {
        return new RequireRoleAttribute(requireAllRoles: true, roles);
    }

    /// <summary>
    /// Creates a role attribute that requires ANY of the specified roles
    /// </summary>
    public static RequireRoleAttribute RequireAny(params string[] roles)
    {
        return new RequireRoleAttribute(requireAllRoles: false, roles);
    }

    public override string ToString()
    {
        string conjunction = RequireAllRoles ? " AND " : " OR ";
        return string.Join(conjunction, RequiredRoles);
    }
}