using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Commands.CreateRole;

/// <summary>
/// Command to create a new role with permissions
/// </summary>
public record CreateRoleCommand(
    string Name,
    string Description,
    List<PermissionDto> Permissions
) : ICommand<CreateRoleResponse>;

/// <summary>
/// Permission data transfer object
/// </summary>
public record PermissionDto(
    string Resource,
    string Action,
    string Scope
);

/// <summary>
/// Response for role creation
/// </summary>
public record CreateRoleResponse(
    Guid Id,
    string Name,
    string Description,
    List<PermissionDto> Permissions,
    DateTime CreatedAt
);