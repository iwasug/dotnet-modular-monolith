using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Api.Services;

/// <summary>
/// Service interface for accessing current user context information
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user ID if authenticated
    /// </summary>
    UserId? CurrentUserId { get; }

    /// <summary>
    /// Gets the current user email if authenticated
    /// </summary>
    string? CurrentUserEmail { get; }

    /// <summary>
    /// Gets the current user name if authenticated
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    /// Gets the current user's role IDs if authenticated
    /// </summary>
    IReadOnlyList<Guid> CurrentUserRoleIds { get; }

    /// <summary>
    /// Gets the current JWT token ID if authenticated
    /// </summary>
    string? CurrentTokenId { get; }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user has any of the specified role IDs
    /// </summary>
    bool HasAnyRole(params Guid[] roleIds);

    /// <summary>
    /// Checks if the current user has all of the specified role IDs
    /// </summary>
    bool HasAllRoles(params Guid[] roleIds);
}