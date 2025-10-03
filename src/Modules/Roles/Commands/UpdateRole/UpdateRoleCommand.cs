using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Commands.UpdateRole;

/// <summary>
/// Command to update an existing role with permission modification
/// </summary>
public record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string Description,
    List<PermissionDto> Permissions
) : ICommand<UpdateRoleResponse>;

/// <summary>
/// Permission data transfer object
/// </summary>
public record PermissionDto(
    string Resource,
    string Action,
    string Scope
);

/// <summary>
/// Response for role update
/// </summary>
public record UpdateRoleResponse(
    Guid Id,
    string Name,
    string Description,
    List<PermissionDto> Permissions,
    DateTime UpdatedAt
);