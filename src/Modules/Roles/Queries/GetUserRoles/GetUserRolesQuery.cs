using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Queries.GetUserRoles;

/// <summary>
/// Query to get all roles assigned to a user for authorization purposes
/// </summary>
public record GetUserRolesQuery(Guid UserId) : IQuery<GetUserRolesResponse>;

/// <summary>
/// Role data transfer object for user roles
/// </summary>
public record UserRoleDto(
    Guid RoleId,
    string RoleName,
    string Description,
    List<PermissionDto> Permissions,
    DateTime AssignedAt,
    Guid? AssignedBy
);

/// <summary>
/// Permission data transfer object
/// </summary>
public record PermissionDto(
    string Resource,
    string Action,
    string Scope
);

/// <summary>
/// Response for user roles query
/// </summary>
public record GetUserRolesResponse(
    Guid UserId,
    List<UserRoleDto> Roles,
    List<PermissionDto> AllPermissions
);