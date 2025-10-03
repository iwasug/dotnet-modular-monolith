using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Queries.GetRole;

/// <summary>
/// Query to get a role by ID with permission details
/// </summary>
public record GetRoleQuery(Guid RoleId) : IQuery<GetRoleResponse>;

/// <summary>
/// Permission data transfer object
/// </summary>
public record PermissionDto(
    string Resource,
    string Action,
    string Scope
);

/// <summary>
/// Response for role query with permission details
/// </summary>
public record GetRoleResponse(
    Guid Id,
    string Name,
    string Description,
    List<PermissionDto> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt
);