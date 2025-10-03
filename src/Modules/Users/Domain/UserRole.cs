using ModularMonolith.Shared.Domain;
using ModularMonolith.Users.Domain.ValueObjects;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain;

/// <summary>
/// Represents the relationship between a user and a role
/// </summary>
public class UserRole : BaseEntity
{
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }
    public UserId? AssignedBy { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public ModularMonolith.Roles.Domain.Role Role { get; private set; } = null!;

    private UserRole() 
    {
        UserId = UserId.From(Guid.Empty);
        RoleId = RoleId.From(Guid.Empty);
    } // For EF Core

    public static UserRole Create(UserId userId, RoleId roleId, UserId? assignedBy = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedBy = assignedBy,
            AssignedAt = DateTime.UtcNow
        };
    }
}