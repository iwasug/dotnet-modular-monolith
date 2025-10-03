using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Roles.Domain.ValueObjects;

/// <summary>
/// Value object representing a permission with Resource-Action-Scope model
/// </summary>
public sealed class Permission : ValueObject
{
    public string Resource { get; }
    public string Action { get; }
    public string Scope { get; }

    public Permission(string resource, string action, string scope)
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

        if (resource.Length > 100)
        {
            throw new ArgumentException("Resource cannot exceed 100 characters", nameof(resource));
        }

        if (action.Length > 50)
        {
            throw new ArgumentException("Action cannot exceed 50 characters", nameof(action));
        }

        if (scope.Length > 50)
        {
            throw new ArgumentException("Scope cannot exceed 50 characters", nameof(scope));
        }

        Resource = resource.Trim().ToLowerInvariant();
        Action = action.Trim().ToLowerInvariant();
        Scope = scope.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Creates a Permission from resource, action, and scope values
    /// </summary>
    public static Permission Create(string resource, string action, string scope) => new(resource, action, scope);

    /// <summary>
    /// Creates a Permission from a formatted string (resource:action:scope)
    /// </summary>
    public static Permission FromString(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
        {
            throw new ArgumentException("Permission string cannot be null or empty", nameof(permissionString));
        }

        var parts = permissionString.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new ArgumentException("Permission string must be in format 'resource:action:scope'", nameof(permissionString));
        }

        return new Permission(parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Checks if this permission matches another permission (supports wildcard matching)
    /// </summary>
    public bool Matches(Permission other)
    {
        if (other == null) return false;

        return MatchesComponent(Resource, other.Resource) &&
               MatchesComponent(Action, other.Action) &&
               MatchesComponent(Scope, other.Scope);
    }

    private static bool MatchesComponent(string thisComponent, string otherComponent)
    {
        return thisComponent == "*" || otherComponent == "*" || thisComponent == otherComponent;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Resource;
        yield return Action;
        yield return Scope;
    }

    public override string ToString() => $"{Resource}:{Action}:{Scope}";

    // Common permission factory methods
    public static class Common
    {
        // User permissions
        public static Permission UserRead => Create("user", "read", "*");
        public static Permission UserWrite => Create("user", "write", "*");
        public static Permission UserDelete => Create("user", "delete", "*");
        public static Permission UserManage => Create("user", "*", "*");

        // Role permissions
        public static Permission RoleRead => Create("role", "read", "*");
        public static Permission RoleWrite => Create("role", "write", "*");
        public static Permission RoleDelete => Create("role", "delete", "*");
        public static Permission RoleManage => Create("role", "*", "*");

        // System permissions
        public static Permission SystemAdmin => Create("*", "*", "*");
        public static Permission SystemRead => Create("*", "read", "*");
    }
}