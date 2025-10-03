using ModularMonolith.Shared.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Roles.Domain;

/// <summary>
/// Role domain entity
/// </summary>
public class Role : BaseEntity
{
    public RoleName Name { get; private set; }
    public string Description { get; private set; }
    // Navigation properties - temporarily commented out to avoid EF Core mapping issues
    // public List<RolePermission> RolePermissions { get; private set; }

    private Role()
    {
        Name = RoleName.From("Temp");
        Description = string.Empty;
        // RolePermissions = new List<RolePermission>();
    }

    /// <summary>
    /// Factory method to create a new role using Guid.CreateVersion7()
    /// </summary>
    public static Role Create(RoleName name, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be null or empty", nameof(description));
        }

        if (description.Length > 500)
        {
            throw new ArgumentException("Description cannot exceed 500 characters", nameof(description));
        }

        return new Role
        {
            Name = name,
            Description = description.Trim(),
            // RolePermissions = new List<RolePermission>()
        };
    }

    /// <summary>
    /// Factory method to create a new role from string values
    /// </summary>
    public static Role Create(string name, string description)
    {
        return Create(RoleName.From(name), description);
    }

    // Navigation properties for permissions
    public List<ModularMonolith.Shared.Domain.Permission> Permissions { get; private set; } = new();

    /// <summary>
    /// Adds a permission to the role
    /// </summary>
    public void AddPermission(ModularMonolith.Shared.Domain.Permission permission)
    {
        if (permission is null)
        {
            throw new ArgumentNullException(nameof(permission));
        }

        if (!Permissions.Any(p => p.Resource == permission.Resource && 
                                 p.Action == permission.Action && 
                                 p.Scope == permission.Scope))
        {
            Permissions.Add(permission);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Removes a permission from the role
    /// </summary>
    public void RemovePermission(ModularMonolith.Shared.Domain.Permission permission)
    {
        if (permission is null)
        {
            throw new ArgumentNullException(nameof(permission));
        }

        var existingPermission = Permissions.FirstOrDefault(p => 
            p.Resource == permission.Resource && 
            p.Action == permission.Action && 
            p.Scope == permission.Scope);
            
        if (existingPermission is not null)
        {
            Permissions.Remove(existingPermission);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Sets all permissions for the role (replaces existing permissions)
    /// </summary>
    public void SetPermissions(List<ModularMonolith.Shared.Domain.Permission> permissions)
    {
        if (permissions is null)
        {
            throw new ArgumentNullException(nameof(permissions));
        }

        Permissions.Clear();
        Permissions.AddRange(permissions);
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if the role has a specific permission
    /// </summary>
    public bool HasPermission(ModularMonolith.Shared.Domain.Permission permission)
    {
        if (permission is null) return false;

        return Permissions.Any(p => p.Resource == permission.Resource && 
                                   p.Action == permission.Action && 
                                   p.Scope == permission.Scope);
    }

    /// <summary>
    /// Updates the role's name and description
    /// </summary>
    public void Update(RoleName name, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be null or empty", nameof(description));
        }

        if (description.Length > 500)
        {
            throw new ArgumentException("Description cannot exceed 500 characters", nameof(description));
        }

        Name = name;
        Description = description.Trim();
        UpdateTimestamp();
    }



    /// <summary>
    /// Gets all permissions
    /// </summary>
    public IReadOnlyList<ModularMonolith.Shared.Domain.Permission> GetPermissions()
    {
        return Permissions.AsReadOnly();
    }
}