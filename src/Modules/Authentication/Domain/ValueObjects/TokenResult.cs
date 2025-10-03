using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Authentication.Domain.ValueObjects;

/// <summary>
/// Value object representing JWT token response
/// </summary>
public sealed class TokenResult : ValueObject
{
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public DateTime ExpiresAt { get; }
    public string TokenType { get; }

    public TokenResult(string accessToken, string refreshToken, DateTime expiresAt, string tokenType = "Bearer")
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token cannot be null or empty", nameof(accessToken));
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future", nameof(expiresAt));
        }

        if (string.IsNullOrWhiteSpace(tokenType))
        {
            throw new ArgumentException("Token type cannot be null or empty", nameof(tokenType));
        }

        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        TokenType = tokenType;
    }

    /// <summary>
    /// Creates a new TokenResult with Bearer token type
    /// </summary>
    public static TokenResult Create(string accessToken, string refreshToken, DateTime expiresAt)
    {
        return new TokenResult(accessToken, refreshToken, expiresAt);
    }

    /// <summary>
    /// Creates a new TokenResult with custom token type
    /// </summary>
    public static TokenResult Create(string accessToken, string refreshToken, DateTime expiresAt, string tokenType)
    {
        return new TokenResult(accessToken, refreshToken, expiresAt, tokenType);
    }

    /// <summary>
    /// Gets the time remaining until token expiration
    /// </summary>
    public TimeSpan TimeUntilExpiration => ExpiresAt - DateTime.UtcNow;

    /// <summary>
    /// Checks if the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccessToken;
        yield return RefreshToken;
        yield return ExpiresAt;
        yield return TokenType;
    }

    public override string ToString()
    {
        return $"{TokenType} {AccessToken}";
    }
}