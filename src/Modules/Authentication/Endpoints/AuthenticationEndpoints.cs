using System.Security.Claims;
using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Commands.Login;
using ModularMonolith.Authentication.Commands.RefreshToken;
using ModularMonolith.Authentication.Commands.Logout;
using ModularMonolith.Authentication.Commands.Register;
using ModularMonolith.Authentication.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

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

        // POST /api/auth/register - User registration (with feature flag)
        auth.MapPost("/register", async (
            [FromBody] RegisterCommand command,
            [FromServices] IValidator<RegisterCommand> validator,
            [FromServices] ICommandHandler<RegisterCommand, RegisterResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IAuthLocalizationService authLocalizationService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Check if registration feature is enabled
            bool isRegistrationEnabled = await featureManager.IsEnabledAsync("UserRegistration");
            if (!isRegistrationEnabled)
            {
                var error = Error.Forbidden("FEATURE_DISABLED", authLocalizationService.GetString("RegistrationDisabled"));
                return Results.Json(ApiResponse<RegisterResponse>.Fail(error.Message, error), statusCode: 403);
            }

            // Add security headers
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
                success => Results.Ok(ApiResponse<RegisterResponse>.Ok(success, "Registration successful")),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(ApiResponse<RegisterResponse>.Fail(error.Message, error)),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<RegisterResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<RegisterResponse>.Fail("Registration failed", error), statusCode: 500)
                }
            );
        })
        .WithName("Register")
        .WithSummary("User registration")
        .WithDescription("Registers a new user account. This endpoint is controlled by the UserRegistration feature flag.")
        .AllowAnonymous()
        .Produces<ApiResponse<RegisterResponse>>(200)
        .ProducesValidationProblem()
        .Produces(403)
        .Produces(409)
        .Produces(500);

        // POST /api/auth/login - User login
        auth.MapPost("/login", async (
            [FromBody] LoginCommand command,
            [FromServices] IValidator<LoginCommand> validator,
            [FromServices] ICommandHandler<LoginCommand, LoginResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IAuthLocalizationService authLocalizationService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Check if login feature is enabled
            if (!await featureManager.IsEnabledAsync("UserLogin"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", authLocalizationService.GetString("LoginDisabled"));
                return Results.Json(ApiResponse<LoginResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<LoginResponse>.Ok(success, "Login successful")),
                error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Json(ApiResponse<LoginResponse>.Fail(error.Message, error), statusCode: 401),
                    ErrorType.Forbidden => Results.Json(ApiResponse<LoginResponse>.Fail(error.Message, error), statusCode: 403),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<LoginResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<LoginResponse>.Fail("Authentication failed", error), statusCode: 500)
                }
            );
        })
        .WithName("Login")
        .WithSummary("User login")
        .WithDescription("Authenticates a user with email and password, returning JWT tokens")
        .AllowAnonymous()
        .Produces<ApiResponse<LoginResponse>>(200)
        .ProducesValidationProblem()
        .Produces(401)
        .Produces(403)
        .Produces(500);

        // POST /api/auth/refresh - Refresh tokens
        auth.MapPost("/refresh", async (
            [FromBody] RefreshTokenCommand command,
            [FromServices] IValidator<RefreshTokenCommand> validator,
            [FromServices] ICommandHandler<RefreshTokenCommand, RefreshTokenResponse> handler,
            [FromServices] IFeatureManager featureManager,
            [FromServices] IAuthLocalizationService authLocalizationService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Check if token refresh feature is enabled
            if (!await featureManager.IsEnabledAsync("TokenRefresh"))
            {
                var error = Error.Forbidden("FEATURE_DISABLED", authLocalizationService.GetString("TokenRefreshDisabled"));
                return Results.Json(ApiResponse<RefreshTokenResponse>.Fail(error.Message, error), statusCode: 403);
            }

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
                success => Results.Ok(ApiResponse<RefreshTokenResponse>.Ok(success, "Token refreshed successfully")),
                error => error.Type switch
                {
                    ErrorType.Unauthorized => Results.Json(ApiResponse<RefreshTokenResponse>.Fail(error.Message, error), statusCode: 401),
                    ErrorType.Validation => Results.BadRequest(ApiResponse<RefreshTokenResponse>.Fail(error.Message, error)),
                    _ => Results.Json(ApiResponse<RefreshTokenResponse>.Fail("Token refresh failed", error), statusCode: 500)
                }
            );
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh authentication tokens")
        .WithDescription("Refreshes JWT tokens using a valid refresh token")
        .AllowAnonymous()
        .Produces<ApiResponse<RefreshTokenResponse>>(200)
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
            return Results.Ok(ApiResponse<LogoutResponse>.Ok(new LogoutResponse(true, "Logout completed successfully"), "Logout completed successfully"));
        })
        .WithName("Logout")
        .WithSummary("User logout")
        .WithDescription("Logs out a user by revoking their refresh token")
        .AllowAnonymous()
        .Produces<ApiResponse<LogoutResponse>>(200)
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
                var error = Error.Unauthorized("NOT_AUTHENTICATED", "User is not authenticated");
                return Results.Json(ApiResponse<object>.Fail("User is not authenticated", error), statusCode: 401);
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

            return Results.Ok(ApiResponse<object>.Ok(userInfo, "User information retrieved successfully"));
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get current user information")
        .WithDescription("Returns information about the currently authenticated user")
        .RequireAuthorization()
        .Produces(200)
        .Produces(401);
    }
}