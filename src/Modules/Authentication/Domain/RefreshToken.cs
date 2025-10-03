using ModularMonolith.Shared.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Domain;

/// <summary>
/// Refresh token entity for JWT token rotation
/// </summary>
public class RefreshToken : BaseEntity
{
    public UserId UserId { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    private RefreshToken() 
    {
        UserId = UserId.From(Guid.Empty);
        Token = string.Empty;
    } // For EF Core

    private RefreshToken(UserId userId, string token, DateTime expiresAt)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    /// <summary>
    /// Creates a new refresh token with UUID v7 for better performance and ordering
    /// </summary>
    public static RefreshToken Create(UserId userId, string token, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));
        }

        return new RefreshToken(userId, token, expiresAt);
    }

    /// <summary>
    /// Revokes the refresh token
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Token is already revoked");
        }

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if the token is valid (not expired and not revoked)
    /// </summary>
    public bool IsValid => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    /// <summary>
    /// Checks if the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}