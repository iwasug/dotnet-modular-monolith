using System.Security.Claims;
using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Commands.Login;
using ModularMonolith.Authentication.Commands.RefreshToken;
using ModularMonolith.Authentication.Commands.Logout;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Authentication.Endpoints;

/// <summary>
/// Authentication API endpoints
/// </summary>
internal static class AuthenticationEndpoints
{
    /// <summary>
    /// Maps authentication endpoints to the application
    /// </summary>
    internal static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        // POST /api/auth/login - User login
        auth.MapPost("/login", async (
            [FromBody] LoginCommand command,
            [FromServices] IValidator<LoginCommand> validator,
            [FromServices] ICommandHandler<LoginCommand, LoginResponse> handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Add security headers for authentication endpoint
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            
            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => 
                {
                    // Set secure cookie options for refresh token if needed
                    var response = Results.Ok(success);
                    return response;
                },
                error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Problem(
                        detail: "Invalid credentials",
                        statusCode: 401,
                        title: "Unauthorized"),
                    ErrorType.Forbidden => Results.Problem(
                        detail: "Account is locked or disabled",
                        statusCode: 403,
                        title: "Forbidden"),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem("Authentication failed", statusCode: 500)
                }
            );
        })
        .WithName("Login")
        .WithSummary("User login")
        .WithDescription("Authenticates a user with email and password, returning JWT tokens")
        .AllowAnonymous()
        .Produces<LoginResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/auth/refresh - Refresh tokens
        auth.MapPost("/refresh", async (
            [FromBody] RefreshTokenCommand command,
            [FromServices] IValidator<RefreshTokenCommand> validator,
            [FromServices] ICommandHandler<RefreshTokenCommand, RefreshTokenResponse> handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Add security headers for token refresh endpoint
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            
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
                    ErrorType.Unauthorized => Results.Problem(
                        detail: "Invalid or expired refresh token",
                        statusCode: 401,
                        title: "Unauthorized"),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.Problem("Token refresh failed", statusCode: 500)
                }
            );
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh authentication tokens")
        .WithDescription("Refreshes JWT tokens using a valid refresh token")
        .AllowAnonymous()
        .Produces<RefreshTokenResponse>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(500);

        // POST /api/auth/logout - User logout
        auth.MapPost("/logout", async (
            [FromBody] LogoutCommand command,
            [FromServices] IValidator<LogoutCommand> validator,
            [FromServices] ICommandHandler<LogoutCommand, LogoutResponse> handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Add security headers for logout endpoint
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            
            // Validate the command
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Execute the command
            var result = await handler.Handle(command, cancellationToken);

            // Always return success for security reasons (don't reveal if token was valid)
            return Results.Ok(new LogoutResponse(true, "Logout completed successfully"));
        })
        .WithName("Logout")
        .WithSummary("User logout")
        .WithDescription("Logs out a user by revoking their refresh token")
        .AllowAnonymous()
        .Produces<LogoutResponse>(200)
        .ProducesValidationProblem()
        .Produces(500);

        // GET /api/auth/me - Get current user info (requires authentication)
        auth.MapGet("/me", (
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            ClaimsPrincipal user = context.User;
            if (!user.Identity?.IsAuthenticated == true)
            {
                return Results.Unauthorized();
            }

            // Extract user information from claims
            string? userId = user.FindFirst("sub")?.Value ?? user.FindFirst("userId")?.Value;
            string? email = user.FindFirst("email")?.Value ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            string? name = user.FindFirst("name")?.Value ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            var userInfo = new
            {
                UserId = userId,
                Email = email,
                Name = name,
                IsAuthenticated = true,
                Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            return Results.Ok(userInfo);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current user information")
        .WithDescription("Returns information about the currently authenticated user")
        .RequireAuthorization()
        .Produces(200)
        .Produces(401);
    }
}