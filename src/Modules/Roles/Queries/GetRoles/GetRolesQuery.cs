using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Roles.Queries.GetRoles;

/// <summary>
/// Query to get roles with filtering support
/// </summary>
public record GetRolesQuery(
    string? NameFilter = null,
    string? PermissionResource = null,
    string? PermissionAction = null,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<GetRolesResponse>;

/// <summary>
/// Role data transfer object
/// </summary>
public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    List<PermissionDto> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt
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
/// Response for roles query with pagination
/// </summary>
public record GetRolesResponse(
    List<RoleDto> Roles,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);