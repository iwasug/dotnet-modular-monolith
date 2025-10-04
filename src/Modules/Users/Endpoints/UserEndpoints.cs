using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Commands.CreateUser;
using ModularMonolith.Users.Commands.UpdateUser;
using ModularMonolith.Users.Commands.DeleteUser;
using ModularMonolith.Users.Queries.GetUser;
using ModularMonolith.Users.Authorization;
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
                return Results.Problem(
                    detail: localizationService.GetString("FeatureDisabled"),
                    statusCode: 403,
                    title: "Feature Disabled");
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
                success => Results.Created($"/api/users/{success.Id}", success),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user with the provided information")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.WRITE)
        .Produces<CreateUserResponse>(201)
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
                return Results.Problem(
                    detail: localizationService.GetString("FeatureDisabled"),
                    statusCode: 403,
                    title: "Feature Disabled");
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
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("GetUser")
        .WithSummary("Get user by ID")
        .WithDescription("Retrieves a user by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.READ)
        .Produces<GetUserResponse>(200)
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
                return Results.Problem(
                    detail: localizationService.GetString("FeatureDisabled"),
                    statusCode: 403,
                    title: "Feature Disabled");
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
        .WithName("UpdateUser")
        .WithSummary("Update an existing user")
        .WithDescription("Updates user information by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.UPDATE)
        .Produces<UpdateUserResponse>(200)
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
                return Results.Problem(
                    detail: localizationService.GetString("FeatureDisabled"),
                    statusCode: 403,
                    title: "Feature Disabled");
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
                success => Results.Ok(success),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem(error.Message, statusCode: 500)
                }
            );
        })
        .WithName("DeleteUser")
        .WithSummary("Delete a user")
        .WithDescription("Soft deletes a user by their unique identifier")
        .RequirePermission(Users.Authorization.UserPermissions.RESOURCE, Users.Authorization.UserPermissions.Actions.DELETE)
        .Produces<DeleteUserResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500);
    }
}