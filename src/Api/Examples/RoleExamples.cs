using ModularMonolith.Roles.Commands.CreateRole;
using ModularMonolith.Roles.Commands.AssignRoleToUser;
using ModularMonolith.Roles.Queries.GetRole;
using ModularMonolith.Roles.Queries.GetRoles;
using ModularMonolith.Roles.Queries.GetUserRoles;
using ModularMonolith.Roles.Endpoints;
using Swashbuckle.AspNetCore.Filters;

namespace ModularMonolith.Api.Examples;

/// <summary>
/// Example provider for create role command
/// </summary>
public sealed class CreateRoleCommandExample : IExamplesProvider<CreateRoleCommand>
{
    public CreateRoleCommand GetExamples()
    {
        return new CreateRoleCommand(
            Name: "Manager",
            Description: "Manager role with team management permissions",
            Permissions: new List<ModularMonolith.Roles.Commands.CreateRole.PermissionDto>
            {
                new("user", "read", "team"),
                new("user", "write", "team"),
                new("role", "read", "team")
            }
        );
    }
}

/// <summary>
/// Example provider for create role response
/// </summary>
public sealed class CreateRoleResponseExample : IExamplesProvider<CreateRoleResponse>
{
    public CreateRoleResponse GetExamples()
    {
        return new CreateRoleResponse(
            Id: Guid.Parse("87654321-4321-4321-4321-210987654321"),
            Name: "Manager",
            Description: "Manager role with team management permissions",
            Permissions: new List<ModularMonolith.Roles.Commands.CreateRole.PermissionDto>
            {
                new("user", "read", "team"),
                new("user", "write", "team"),
                new("role", "read", "team")
            },
            CreatedAt: DateTime.UtcNow
        );
    }
}

/// <summary>
/// Example provider for update role request
/// </summary>
public sealed class UpdateRoleRequestExample : IExamplesProvider<UpdateRoleRequest>
{
    public UpdateRoleRequest GetExamples()
    {
        return new UpdateRoleRequest(
            Name: "Senior Manager",
            Description: "Senior manager role with extended permissions",
            Permissions: new List<ModularMonolith.Roles.Endpoints.PermissionDto>
            {
                new("user", "read", "organization"),
                new("user", "write", "organization"),
                new("role", "read", "organization"),
                new("role", "assign", "team")
            }
        );
    }
}

/// <summary>
/// Example provider for get role response
/// </summary>
public sealed class GetRoleResponseExample : IExamplesProvider<GetRoleResponse>
{
    public GetRoleResponse GetExamples()
    {
        return new GetRoleResponse(
            Id: Guid.Parse("87654321-4321-4321-4321-210987654321"),
            Name: "Manager",
            Description: "Manager role with team management permissions",
            Permissions: new List<ModularMonolith.Roles.Queries.GetRole.PermissionDto>
            {
                new("user", "read", "team"),
                new("user", "write", "team"),
                new("role", "read", "team")
            },
            CreatedAt: DateTime.UtcNow.AddDays(-10),
            UpdatedAt: DateTime.UtcNow.AddDays(-1)
        );
    }
}

/// <summary>
/// Example provider for get roles response
/// </summary>
public sealed class GetRolesResponseExample : IExamplesProvider<GetRolesResponse>
{
    public GetRolesResponse GetExamples()
    {
        return new GetRolesResponse(
            Roles: new List<ModularMonolith.Roles.Queries.GetRoles.RoleDto>
            {
                new(
                    Id: Guid.Parse("87654321-4321-4321-4321-210987654321"),
                    Name: "Manager",
                    Description: "Manager role with team management permissions",
                    Permissions: new List<ModularMonolith.Roles.Queries.GetRoles.PermissionDto>
                    {
                        new("user", "read", "team"),
                        new("user", "write", "team"),
                        new("role", "read", "team")
                    },
                    CreatedAt: DateTime.UtcNow.AddDays(-10),
                    UpdatedAt: DateTime.UtcNow.AddDays(-1)
                ),
                new(
                    Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name: "User",
                    Description: "Basic user role",
                    Permissions: new List<ModularMonolith.Roles.Queries.GetRoles.PermissionDto>
                    {
                        new("user", "read", "own")
                    },
                    CreatedAt: DateTime.UtcNow.AddDays(-30),
                    UpdatedAt: DateTime.UtcNow.AddDays(-30)
                )
            },
            TotalCount: 2,
            PageNumber: 1,
            PageSize: 10,
            TotalPages: 1
        );
    }
}

/// <summary>
/// Example provider for get user roles response
/// </summary>
public sealed class GetUserRolesResponseExample : IExamplesProvider<GetUserRolesResponse>
{
    public GetUserRolesResponse GetExamples()
    {
        return new GetUserRolesResponse(
            UserId: Guid.Parse("12345678-1234-1234-1234-123456789012"),
            Roles: new List<ModularMonolith.Roles.Queries.GetUserRoles.UserRoleDto>
            {
                new(
                    RoleId: Guid.Parse("87654321-4321-4321-4321-210987654321"),
                    RoleName: "Manager",
                    Description: "Manager role with team management permissions",
                    Permissions: new List<ModularMonolith.Roles.Queries.GetUserRoles.PermissionDto>
                    {
                        new("user", "read", "team"),
                        new("user", "write", "team"),
                        new("role", "read", "team")
                    },
                    AssignedAt: DateTime.UtcNow.AddDays(-5),
                    AssignedBy: Guid.Parse("99999999-9999-9999-9999-999999999999")
                ),
                new(
                    RoleId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    RoleName: "User",
                    Description: "Basic user role",
                    Permissions: new List<ModularMonolith.Roles.Queries.GetUserRoles.PermissionDto>
                    {
                        new("user", "read", "own")
                    },
                    AssignedAt: DateTime.UtcNow.AddDays(-30),
                    AssignedBy: Guid.Parse("99999999-9999-9999-9999-999999999999")
                )
            },
            AllPermissions: new List<ModularMonolith.Roles.Queries.GetUserRoles.PermissionDto>
            {
                new("user", "read", "team"),
                new("user", "write", "team"),
                new("role", "read", "team"),
                new("user", "read", "own")
            }
        );
    }
}

/// <summary>
/// Example provider for assign role to user response
/// </summary>
public sealed class AssignRoleToUserResponseExample : IExamplesProvider<AssignRoleToUserResponse>
{
    public AssignRoleToUserResponse GetExamples()
    {
        return new AssignRoleToUserResponse(
            UserId: Guid.Parse("12345678-1234-1234-1234-123456789012"),
            RoleId: Guid.Parse("87654321-4321-4321-4321-210987654321"),
            RoleName: "Manager",
            AssignedBy: Guid.Parse("99999999-9999-9999-9999-999999999999"),
            AssignedAt: DateTime.UtcNow
        );
    }
}