using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Commands.CreateRole;
using ModularMonolith.Roles.Commands.UpdateRole;
using ModularMonolith.Roles.Commands.DeleteRole;
using ModularMonolith.Roles.Commands.AssignRoleToUser;
using ModularMonolith.Roles.Queries.GetRole;
using ModularMonolith.Roles.Queries.GetRoles;
using ModularMonolith.Roles.Queries.GetUserRoles;
using ModularMonolith.Roles.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

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
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<CreateRoleResponse>.Fail(error.Message, error), statusCode: 403);
            }

            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Created($"/api/roles/{success.Id}", ApiResponse<CreateRoleResponse>.Ok(success, "Role created successfully")),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(ApiResponse<CreateRoleResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<CreateRoleResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<CreateRoleResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("CreateRole")
        .WithSummary("Create a new role")
        .WithDescription("Creates a new role with the specified permissions")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.WRITE)
        .Produces<ApiResponse<CreateRoleResponse>>(201)
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
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<GetRoleResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<GetRoleResponse>.Ok(success, "Role retrieved successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<GetRoleResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<GetRoleResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<GetRoleResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("GetRole")
        .WithSummary("Get role by ID")
        .WithDescription("Retrieves a role by its unique identifier with permission details")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<ApiResponse<GetRoleResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // GET /api/roles - Get roles with filtering
        roles.MapGet("/", async (
            [FromServices] IValidator<GetRolesQuery> validator,
            [FromServices] IQueryHandler<GetRolesQuery, GetRolesResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken,
            [FromQuery] string? nameFilter = null,
            [FromQuery] string? permissionResource = null,
            [FromQuery] string? permissionAction = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<GetRolesResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<GetRolesResponse>.Ok(success, "Roles retrieved successfully")),
                error => error.Type switch
                {
                    ErrorType.Validation => Results.BadRequest(ApiResponse<GetRolesResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<GetRolesResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("GetRoles")
        .WithSummary("Get roles with filtering")
        .WithDescription("Retrieves roles with optional filtering and pagination")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<ApiResponse<GetRolesResponse>>(200)
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
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<UpdateRoleResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<UpdateRoleResponse>.Ok(success, "Role updated successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<UpdateRoleResponse>.Fail(error.Message, error)),
                    ErrorType.Conflict => Results.Conflict(ApiResponse<UpdateRoleResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<UpdateRoleResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<UpdateRoleResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("UpdateRole")
        .WithSummary("Update role")
        .WithDescription("Updates an existing role with new permissions")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.WRITE)
        .Produces<ApiResponse<UpdateRoleResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

        // DELETE /api/roles/{id} - Delete role
        roles.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IValidator<DeleteRoleCommand> validator,
            [FromServices] ICommandHandler<DeleteRoleCommand, DeleteRoleResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<DeleteRoleResponse>.Fail(error.Message, error), statusCode: 403);
            }

            var command = new DeleteRoleCommand(id);

            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(ApiResponse<DeleteRoleResponse>.Ok(success, "Role deleted successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<DeleteRoleResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<DeleteRoleResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<DeleteRoleResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("DeleteRole")
        .WithSummary("Delete a role")
        .WithDescription("Soft deletes a role by its unique identifier")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.DELETE)
        .Produces<ApiResponse<DeleteRoleResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // POST /api/roles/{roleId}/assign/{userId} - Assign role to user
        roles.MapPost("/{roleId:guid}/assign/{userId:guid}", async (
            [FromRoute] Guid roleId,
            [FromRoute] Guid userId,
            [FromServices] IValidator<AssignRoleToUserCommand> validator,
            [FromServices] ICommandHandler<AssignRoleToUserCommand, AssignRoleToUserResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<AssignRoleToUserResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<AssignRoleToUserResponse>.Ok(success, "Role assigned to user successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<AssignRoleToUserResponse>.Fail(error.Message, error)),
                    ErrorType.Conflict => Results.Conflict(ApiResponse<AssignRoleToUserResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<AssignRoleToUserResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<AssignRoleToUserResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("AssignRoleToUser")
        .WithSummary("Assign role to user")
        .WithDescription("Assigns a role to a user")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.ASSIGN)
        .Produces<ApiResponse<AssignRoleToUserResponse>>(200)
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
            [FromServices] IFeatureManager featureManager,
            [FromServices] IRoleLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if role management feature is enabled
            if (!await featureManager.IsEnabledAsync("RoleManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<GetUserRolesResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<GetUserRolesResponse>.Ok(success, "User roles retrieved successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<GetUserRolesResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<GetUserRolesResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<GetUserRolesResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("GetUserRoles")
        .WithSummary("Get user roles")
        .WithDescription("Retrieves all roles assigned to a specific user")
        .RequirePermission(Roles.Authorization.RolePermissions.RESOURCE, Roles.Authorization.RolePermissions.Actions.READ)
        .Produces<ApiResponse<GetUserRolesResponse>>(200)
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