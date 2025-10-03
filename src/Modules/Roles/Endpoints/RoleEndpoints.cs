using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Commands.CreateRole;
using ModularMonolith.Roles.Commands.UpdateRole;
using ModularMonolith.Roles.Commands.AssignRoleToUser;
using ModularMonolith.Roles.Queries.GetRole;
using ModularMonolith.Roles.Queries.GetRoles;
using ModularMonolith.Roles.Queries.GetUserRoles;
using ModularMonolith.Roles.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Roles.Endpoints;

/// <summary>
/// Role API endpoints
/// </summary>
internal static class RoleEndpoints
{
    /// <summary>
    /// Maps role endpoints to the application
    /// </summary>
    internal static void MapRoleEndpoints(this WebApplication app)
    {
        var roles = app.MapGroup("/api/roles")
            .WithTags("Roles");

        // POST /api/roles - Create role
        roles.MapPost("/", async (
            [FromBody] CreateRoleCommand command,
            [FromServices] IValidator<CreateRoleCommand> validator,
            [FromServices] ICommandHandler<CreateRoleCommand, CreateRoleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Created($"/api/roles/{success.Id}", success),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("CreateRole")
        .WithSummary("Create a new role")
        .WithDescription("Creates a new role with the specified permissions")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.WRITE)
        .Produces<CreateRoleResponse>(201)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(409)
        .Produces(500);

        // GET /api/roles/{id} - Get role by ID
        roles.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IValidator<GetRoleQuery> validator,
            [FromServices] IQueryHandler<GetRoleQuery, GetRoleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRoleQuery(id);

            // Validate the query
            var validationResult = await validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the query
            var result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("GetRole")
        .WithSummary("Get role by ID")
        .WithDescription("Retrieves a role by its unique identifier with permission details")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<GetRoleResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // GET /api/roles - Get roles with filtering
        roles.MapGet("/", async (
            [FromServices] IValidator<GetRolesQuery> validator,
            [FromServices] IQueryHandler<GetRolesQuery, GetRolesResponse> handler,
            CancellationToken cancellationToken,
            [FromQuery] string? nameFilter = null,
            [FromQuery] string? permissionResource = null,
            [FromQuery] string? permissionAction = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var query = new GetRolesQuery(nameFilter, permissionResource, permissionAction, pageNumber, pageSize);

            // Validate the query
            var validationResult = await validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the query
            var result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("GetRoles")
        .WithSummary("Get roles with filtering")
        .WithDescription("Retrieves roles with optional filtering and pagination")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<GetRolesResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // PUT /api/roles/{id} - Update role
        roles.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateRoleRequest request,
            [FromServices] IValidator<UpdateRoleCommand> validator,
            [FromServices] ICommandHandler<UpdateRoleCommand, UpdateRoleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var commandPermissions = request.Permissions
                .Select(p => new ModularMonolith.Roles.Commands.UpdateRole.PermissionDto(p.Resource, p.Action, p.Scope))
                .ToList();
            var command = new UpdateRoleCommand(id, request.Name, request.Description, commandPermissions);

            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("UpdateRole")
        .WithSummary("Update role")
        .WithDescription("Updates an existing role with new permissions")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.WRITE)
        .Produces<UpdateRoleResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

        // POST /api/roles/{roleId}/assign/{userId} - Assign role to user
        roles.MapPost("/{roleId:guid}/assign/{userId:guid}", async (
            [FromRoute] Guid roleId,
            [FromRoute] Guid userId,
            [FromServices] IValidator<AssignRoleToUserCommand> validator,
            [FromServices] ICommandHandler<AssignRoleToUserCommand, AssignRoleToUserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignRoleToUserCommand(userId, roleId);

            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("AssignRoleToUser")
        .WithSummary("Assign role to user")
        .WithDescription("Assigns a role to a user")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.ASSIGN)
        .Produces<AssignRoleToUserResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

        // GET /api/roles/users/{userId} - Get user roles
        roles.MapGet("/users/{userId:guid}", async (
            [FromRoute] Guid userId,
            [FromServices] IValidator<GetUserRolesQuery> validator,
            [FromServices] IQueryHandler<GetUserRolesQuery, GetUserRolesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserRolesQuery(userId);

            // Validate the query
            var validationResult = await validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the query
            var result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("GetUserRoles")
        .WithSummary("Get user roles")
        .WithDescription("Retrieves all roles assigned to a specific user")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<GetUserRolesResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);
    }
}

/// <summary>
/// Request model for updating a role (excludes the ID which comes from the route)
/// </summary>
public record UpdateRoleRequest(
    string Name,
    string Description,
    List<PermissionDto> Permissions
);

/// <summary>
/// Permission data transfer object
/// </summary>
public record PermissionDto(
    string Resource,
    string Action,
    string Scope
);