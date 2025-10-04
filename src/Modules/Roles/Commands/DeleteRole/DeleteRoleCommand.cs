using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Commands.DeleteRole;

/// <summary>
/// Command to delete a role (soft delete)
/// </summary>
public record DeleteRoleCommand(
    Guid Id
) : ICommand<DeleteRoleResponse>;

/// <summary>
/// Response for role deletion
/// </summary>
public record DeleteRoleResponse(
    Guid Id,
    DateTime DeletedAt
);
