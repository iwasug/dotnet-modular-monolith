using ModularMonolith.Shared.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Roles.Domain;

/// <summary>
/// Represents the relationship between a role and a permission
/// </summary>
public class RolePermission : BaseEntity
{
    public RoleId RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    // Navigation properties - temporarily commented out to avoid EF Core mapping issues
    // public Role Role { get; private set; } = null!;
    public ModularMonolith.Shared.Domain.Permission Permission { get; private set; } = null!;

    private RolePermission() 
    {
        RoleId = RoleId.From(Guid.Empty);
    } // For EF Core

    public static RolePermission Create(RoleId roleId, Guid permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}