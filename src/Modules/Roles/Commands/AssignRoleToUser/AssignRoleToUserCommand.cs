using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Commands.AssignRoleToUser;

/// <summary>
/// Command to assign a role to a user
/// </summary>
public record AssignRoleToUserCommand(
    Guid UserId,
    Guid RoleId,
    Guid? AssignedBy = null
) : ICommand<AssignRoleToUserResponse>;

/// <summary>
/// Response for role assignment
/// </summary>
public record AssignRoleToUserResponse(
    Guid UserId,
    Guid RoleId,
    string RoleName,
    Guid? AssignedBy,
    DateTime AssignedAt
);