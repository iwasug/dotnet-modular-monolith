using ModularMonolith.Authentication.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Domain.Services;

/// <summary>
/// Service interface for user authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's plain text password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens if successful</returns>
    Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes authentication tokens using a valid refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to use for generating new tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with new tokens if successful</returns>
    Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the logout operation</returns>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a user exists and is active
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user exists and is active, false otherwise</returns>
    Task<bool> IsUserActiveAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login timestamp for a user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the update operation</returns>
    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the revocation operation</returns>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}