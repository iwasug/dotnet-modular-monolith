using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Commands.CreateUser;
using ModularMonolith.Users.Commands.UpdateUser;
using ModularMonolith.Users.Commands.DeleteUser;
using ModularMonolith.Users.Queries.GetUser;
using ModularMonolith.Users.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace ModularMonolith.Users.Endpoints;

/// <summary>
/// User API endpoints
/// </summary>
internal static class UserEndpoints
{
    /// <summary>
    /// Maps user endpoints to the application
    /// </summary>
    internal static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users")
            .WithTags("Users");

        // POST /api/users - Create user
        users.MapPost("/", async (
            [FromBody] CreateUserCommand command,
            [FromServices] IValidator<CreateUserCommand> validator,
            [FromServices] ICommandHandler<CreateUserCommand, CreateUserResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IUserLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if user management feature is enabled
            if (!await featureManager.IsEnabledAsync("UserManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<CreateUserResponse>.Fail(error.Message, error), statusCode: 403);
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
                success => Results.Created($"/api/users/{success.Id}", ApiResponse<CreateUserResponse>.Ok(success, "User created successfully")),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(ApiResponse<CreateUserResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<CreateUserResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<CreateUserResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user with the provided information")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.WRITE)
        .Produces<ApiResponse<CreateUserResponse>>(201)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(409)
        .Produces(500);

        // GET /api/users/{id} - Get user by ID
        users.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IValidator<GetUserQuery> validator,
            [FromServices] IQueryHandler<GetUserQuery, GetUserResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IUserLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if user management feature is enabled
            if (!await featureManager.IsEnabledAsync("UserManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<GetUserResponse>.Fail(error.Message, error), statusCode: 403);
            }

            var query = new GetUserQuery(id);

            // Validate the query
            var validationResult = await validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the query
            var result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(ApiResponse<GetUserResponse>.Ok(success, "User retrieved successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<GetUserResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<GetUserResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<GetUserResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("GetUser")
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves a user by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.READ)
        .Produces<ApiResponse<GetUserResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);

        // PUT /api/users/{id} - Update user
        users.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateUserCommand command,
            [FromServices] IValidator<UpdateUserCommand> validator,
            [FromServices] ICommandHandler<UpdateUserCommand, UpdateUserResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IUserLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if user management feature is enabled
            if (!await featureManager.IsEnabledAsync("UserManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<UpdateUserResponse>.Fail(error.Message, error), statusCode: 403);
            }

            // Ensure route ID matches command ID
            if (id != command.Id)
            {
                return Results.BadRequest(new { Code = "ID_MISMATCH", Message = "Route ID does not match command ID" });
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
                success => Results.Ok(ApiResponse<UpdateUserResponse>.Ok(success, "User updated successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<UpdateUserResponse>.Fail(error.Message, error)),
                    ErrorType.Conflict => Results.Conflict(ApiResponse<UpdateUserResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<UpdateUserResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<UpdateUserResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("UpdateUser")
        .WithSummary("Update an existing user")
        .WithDescription("Updates user information by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.UPDATE)
        .Produces<ApiResponse<UpdateUserResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(409)
        .Produces(500);

        // DELETE /api/users/{id} - Delete user
        users.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IValidator<DeleteUserCommand> validator,
            [FromServices] ICommandHandler<DeleteUserCommand, DeleteUserResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IUserLocalizationService localizationService,
            CancellationToken cancellationToken) =>
        {
            // Check if user management feature is enabled
            if (!await featureManager.IsEnabledAsync("UserManagement"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", localizationService.GetString("FeatureDisabled"));
                return Results.Json(ApiResponse<DeleteUserResponse>.Fail(error.Message, error), statusCode: 403);
            }

            var command = new DeleteUserCommand(id);

            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(ApiResponse<DeleteUserResponse>.Ok(success, "User deleted successfully")),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(ApiResponse<DeleteUserResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<DeleteUserResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<DeleteUserResponse>.Fail(error.Message, error), statusCode: 500)
                }
            );
        })
        .WithName("DeleteUser")
        .WithSummary("Delete a user")
        .WithDescription("Soft deletes a user by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.DELETE)
        .Produces<ApiResponse<DeleteUserResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);
    }
}