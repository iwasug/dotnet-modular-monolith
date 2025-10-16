using Microsoft.AspNetCore.Mvc;
using ModularMonolith.Api.Authorization.Attributes;
using ModularMonolith.Api.Services;

namespace ModularMonolith.Api.Controllers;

/// <summary>
/// Example controller demonstrating authorization usage
/// </summary>
[ApiController]
[Route("api/[controller]")]
internal sealed class ExampleAuthorizationController(
    IUserContext userContext,
    ILogger<ExampleAuthorizationController> logger)
    : ControllerBase
{
    /// <summary>
    /// Public endpoint - no authorization required
    /// </summary>
    [HttpGet("public")]
    public IActionResult GetPublicData()
    {
        return Ok(new { Message = "This is public data", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Authenticated endpoint - requires valid JWT token
    /// </summary>
    [HttpGet("authenticated")]
    [RequirePermission("example", "read")]
    public IActionResult GetAuthenticatedData()
    {
        return Ok(new 
        { 
            Message = "This is authenticated data", 
            UserId = userContext.CurrentUserId?.Value,
            UserEmail = userContext.CurrentUserEmail,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// Admin only endpoint - requires admin role
    /// </summary>
    [HttpGet("admin")]
    [RequireRole("admin")]
    public IActionResult GetAdminData()
    {
        return Ok(new 
        { 
            Message = "This is admin-only data", 
            UserId = userContext.CurrentUserId?.Value,
            UserRoles = userContext.CurrentUserRoleIds,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// User management endpoint - requires user write permission
    /// </summary>
    [HttpPost("users")]
    [RequirePermission("user", "write")]
    public IActionResult CreateUser([FromBody] object userData)
    {
        logger.LogInformation("User {UserId} is creating a new user", userContext.CurrentUserId?.Value);
        
        return Ok(new 
        { 
            Message = "User creation authorized", 
            CreatedBy = userContext.CurrentUserId?.Value,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// System admin endpoint - requires system admin permission
    /// </summary>
    [HttpDelete("system/reset")]
    [RequirePermission("*", "*")]
    public IActionResult ResetSystem()
    {
        logger.LogWarning("System reset requested by user {UserId}", userContext.CurrentUserId?.Value);
        
        return Ok(new 
        { 
            Message = "System reset authorized (not actually performed in this example)", 
            RequestedBy = userContext.CurrentUserId?.Value,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// Multiple roles endpoint - requires either admin or moderator role
    /// </summary>
    [HttpGet("moderation")]
    [RequireRole("admin", "moderator")]
    public IActionResult GetModerationData()
    {
        return Ok(new 
        { 
            Message = "This is moderation data", 
            UserId = userContext.CurrentUserId?.Value,
            UserRoles = userContext.CurrentUserRoleIds,
            Timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// Current user info endpoint - shows current user context
    /// </summary>
    [HttpGet("me")]
    [RequirePermission("user", "read", "self")]
    public IActionResult GetCurrentUser()
    {
        if (!userContext.IsAuthenticated)
        {
            return Unauthorized();
        }

        return Ok(new 
        { 
            UserId = userContext.CurrentUserId?.Value,
            Email = userContext.CurrentUserEmail,
            Name = userContext.CurrentUserName,
            RoleIds = userContext.CurrentUserRoleIds,
            TokenId = userContext.CurrentTokenId,
            IsAuthenticated = userContext.IsAuthenticated,
            Timestamp = DateTime.UtcNow 
        });
    }
}