namespace ModularMonolith.Shared.Domain;

/// <summary>
/// Represents a permission in the system
/// </summary>
public class Permission : BaseEntity
{
    public string Resource { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;

    private Permission() { } // For EF Core

    public static Permission Create(string resource, string action, string scope)
    {
        return new Permission
        {
            Resource = resource,
            Action = action,
            Scope = scope
        };
    }

    public override string ToString() => $"{Resource}:{Action}:{Scope}";
}