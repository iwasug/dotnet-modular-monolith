using System.Security.Claims;
using ModularMonolith.Authentication.Domain.Services;

namespace ModularMonolith.Api.Middleware;

/// <summary>
/// JWT authentication middleware for validating tokens and setting up user context
/// </summary>
internal sealed class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        try
        {
            AuthenticateRequest(context, tokenService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JWT authentication");
        }

        await _next(context);
    }

    private void AuthenticateRequest(HttpContext context, ITokenService tokenService)
    {
        var token = ExtractTokenFromHeader(context);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("No JWT token found in request");
            return;
        }

        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "JwtAuthentication",
            ["Path"] = context.Request.Path,
            ["Method"] = context.Request.Method
        });

        _logger.LogDebug("Validating JWT token");

        var principal = tokenService.ValidateToken(token);
        if (principal is null)
        {
            _logger.LogWarning("Invalid JWT token provided");
            return;
        }

        // Set the user principal
        context.User = principal;

        // Extract user information and add to context
        var userId = GetUserIdFromPrincipal(principal);
        if (userId.HasValue)
        {
            context.Items["UserId"] = userId.Value;
            _logger.LogDebug("Successfully authenticated user {UserId}", userId.Value);
        }

        // Extract additional claims for easier access
        ExtractAndSetClaims(context, principal);

        _logger.LogDebug("JWT authentication completed successfully");
    }

    private static string? ExtractTokenFromHeader(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return null;
        }

        // Check if it's a Bearer token
        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Extract the token part (remove "Bearer " prefix)
        return authorizationHeader["Bearer ".Length..].Trim();
    }

    private static Guid? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return null;
        }

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static void ExtractAndSetClaims(HttpContext context, ClaimsPrincipal principal)
    {
        // Extract email
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            context.Items["UserEmail"] = email;
        }

        // Extract name
        var name = principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(name))
        {
            context.Items["UserName"] = name;
        }

        // Extract role IDs
        var roleIds = principal.FindAll("role_id")
            .Select(c => c.Value)
            .Where(v => Guid.TryParse(v, out _))
            .Select(v => Guid.Parse(v))
            .ToList();

        if (roleIds.Count > 0)
        {
            context.Items["UserRoleIds"] = roleIds;
        }

        // Extract JWT ID for token tracking
        var jti = principal.FindFirst("jti")?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            context.Items["TokenId"] = jti;
        }
    }
}