using Microsoft.AspNetCore.Authorization;

namespace ModularMonolith.Api.Authorization.Requirements;

/// <summary>
/// Authorization requirement for role-based access control
/// </summary>
internal sealed class RoleRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> RequiredRoles { get; }
    public bool RequireAllRoles { get; }

    public RoleRequirement(IEnumerable<string> requiredRoles, bool requireAllRoles = false)
    {
        if (requiredRoles is null)
        {
            throw new ArgumentNullException(nameof(requiredRoles));
        }

        List<string> roles = requiredRoles.Where(r => !string.IsNullOrWhiteSpace(r))
                                .Select(r => r.Trim().ToLowerInvariant())
                                .Distinct()
                                .ToList();

        if (roles.Count == 0)
        {
            throw new ArgumentException("At least one role must be specified", nameof(requiredRoles));
        }

        RequiredRoles = roles.AsReadOnly();
        RequireAllRoles = requireAllRoles;
    }

    public RoleRequirement(string requiredRole) : this(new[] { requiredRole }, false)
    {
    }

    public RoleRequirement(params string[] requiredRoles) : this(requiredRoles.AsEnumerable(), false)
    {
    }

    /// <summary>
    /// Creates a role requirement that requires ALL specified roles
    /// </summary>
    public static RoleRequirement RequireAll(params string[] roles)
    {
        return new RoleRequirement(roles, requireAllRoles: true);
    }

    /// <summary>
    /// Creates a role requirement that requires ANY of the specified roles
    /// </summary>
    public static RoleRequirement RequireAny(params string[] roles)
    {
        return new RoleRequirement(roles, requireAllRoles: false);
    }

    public override string ToString()
    {
        string conjunction = RequireAllRoles ? " AND " : " OR ";
        return string.Join(conjunction, RequiredRoles);
    }
}