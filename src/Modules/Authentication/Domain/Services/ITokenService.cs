using System.Security.Claims;
using ModularMonolith.Authentication.Domain.ValueObjects;
using ModularMonolith.Users.Domain;

namespace ModularMonolith.Authentication.Domain.Services;

/// <summary>
/// Service interface for JWT token operations
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates access and refresh tokens for a user
    /// </summary>
    /// <param name="user">The user to generate tokens for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token result containing access and refresh tokens</returns>
    Task<TokenResult> GenerateTokensAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes tokens using a valid refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to use for generating new tokens</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New token result with refreshed tokens</returns>
    Task<TokenResult> RefreshTokensAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token and its associated access token
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the revocation operation</returns>
    Task RevokeTokensAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token and returns the claims principal
    /// </summary>
    /// <param name="token">The access token to validate</param>
    /// <returns>Claims principal if token is valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extracts user ID from a valid access token
    /// </summary>
    /// <param name="token">The access token to extract user ID from</param>
    /// <returns>User ID if token is valid and contains user ID claim, null otherwise</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Checks if an access token is expired
    /// </summary>
    /// <param name="token">The access token to check</param>
    /// <returns>True if token is expired, false otherwise</returns>
    bool IsTokenExpired(string token);
}