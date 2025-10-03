using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Domain;

/// <summary>
/// Repository interface for refresh token operations with async methods and CancellationToken support
/// Provides comprehensive CRUD operations and token management following repository pattern
/// </summary>
public interface IRefreshTokenRepository
{
    // Query operations
    /// <summary>
    /// Gets a refresh token by its unique identifier
    /// </summary>
    Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a refresh token by its token value
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all refresh tokens for a specific user
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all active (non-revoked, non-expired) refresh tokens for a user
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all expired refresh tokens
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets refresh tokens with pagination support
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Command operations
    /// <summary>
    /// Adds a new refresh token to the repository
    /// </summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing refresh token in the repository
    /// </summary>
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a refresh token from the repository (hard delete)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a refresh token from the repository by token value
    /// </summary>
    Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revokes a refresh token by marking it as revoked
    /// </summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    Task RevokeAllForUserAsync(UserId userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes all expired refresh tokens (cleanup operation)
    /// </summary>
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);

    // Existence checks
    /// <summary>
    /// Checks if a refresh token exists by its unique identifier
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a refresh token exists by its token value
    /// </summary>
    Task<bool> ExistsByTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a refresh token is valid (exists, not revoked, not expired)
    /// </summary>
    Task<bool> IsValidTokenAsync(string token, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of refresh tokens in the system
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of active refresh tokens in the system
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of refresh tokens for a specific user
    /// </summary>
    Task<int> GetCountByUserAsync(UserId userId, CancellationToken cancellationToken = default);
}