using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Commands.CreateUser;
using ModularMonolith.Users.Queries.GetUser;
using ModularMonolith.Users.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            CancellationToken cancellationToken) =>
        {
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
    }
}