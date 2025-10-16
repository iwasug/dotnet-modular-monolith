using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Infrastructure;

/// <summary>
/// Cached refresh token repository implementation using cache-aside pattern
/// </summary>
public sealed class CachedRefreshTokenRepository(
    IRefreshTokenRepository repository,
    ICacheService cacheService,
    ILogger<CachedRefreshTokenRepository> logger)
    : IRefreshTokenRepository
{
    // Cache key patterns
    private const string TokenByIdKey = "token:id:{0}";
    private const string TokenByTokenKey = "token:token:{0}";
    private const string TokensByUserKey = "tokens:user:{0}";
    private const string ActiveTokensByUserKey = "tokens:user:active:{0}";
    private const string ExpiredTokensKey = "tokens:expired";
    private const string PagedTokensKey = "tokens:paged:{0}:{1}";
    private const string TokenCountKey = "tokens:count";
    private const string ActiveTokenCountKey = "tokens:count:active";
    private const string TokenCountByUserKey = "tokens:count:user:{0}";
    private const string TokenExistsKey = "token:exists:id:{0}";
    private const string TokenExistsByTokenKey = "token:exists:token:{0}";
    private const string TokenValidKey = "token:valid:{0}";

    // Cache tags for invalidation
    private const string TokensTag = "tokens";
    private const string UserTokensTag = "user-tokens:{0}";

    // Cache expiration times
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CountExpiration = TimeSpan.FromMinutes(15);

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenByIdKey, id);
        
        var cachedToken = await cacheService.GetAsync<RefreshToken>(cacheKey, cancellationToken);
        if (cachedToken is not null)
        {
            logger.LogDebug("Cache hit for refresh token ID {TokenId}", id);
            return cachedToken;
        }

        logger.LogDebug("Cache miss for refresh token ID {TokenId}, fetching from database", id);
        var token = await repository.GetByIdAsync(id, cancellationToken);
        
        if (token is not null)
        {
            await cacheService.SetAsync(cacheKey, token, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached refresh token with ID {TokenId}", id);
        }

        return token;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenByTokenKey, GetTokenHash(token));
        
        var cachedToken = await cacheService.GetAsync<RefreshToken>(cacheKey, cancellationToken);
        if (cachedToken is not null)
        {
            logger.LogDebug("Cache hit for refresh token by token value");
            return cachedToken;
        }

        logger.LogDebug("Cache miss for refresh token by token value, fetching from database");
        var refreshToken = await repository.GetByTokenAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await cacheService.SetAsync(cacheKey, refreshToken, DefaultExpiration, cancellationToken);
            // Also cache by ID for consistency
            var idCacheKey = string.Format(TokenByIdKey, refreshToken.Id);
            await cacheService.SetAsync(idCacheKey, refreshToken, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached refresh token by token value");
        }

        return refreshToken;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokensByUserKey, userId.Value);
        
        var cachedTokens = await cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            logger.LogDebug("Cache hit for user tokens {UserId}", userId.Value);
            return cachedTokens;
        }

        logger.LogDebug("Cache miss for user tokens {UserId}, fetching from database", userId.Value);
        var tokens = await repository.GetByUserIdAsync(userId, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached user tokens for {UserId} ({Count} tokens)", userId.Value, tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(ActiveTokensByUserKey, userId.Value);
        
        var cachedTokens = await cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            logger.LogDebug("Cache hit for active user tokens {UserId}", userId.Value);
            return cachedTokens;
        }

        logger.LogDebug("Cache miss for active user tokens {UserId}, fetching from database", userId.Value);
        var tokens = await repository.GetActiveByUserIdAsync(userId, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached active user tokens for {UserId} ({Count} tokens)", userId.Value, tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var cachedTokens = await cacheService.GetAsync<IReadOnlyList<RefreshToken>>(ExpiredTokensKey, cancellationToken);
        if (cachedTokens is not null)
        {
            logger.LogDebug("Cache hit for expired tokens");
            return cachedTokens;
        }

        logger.LogDebug("Cache miss for expired tokens, fetching from database");
        var tokens = await repository.GetExpiredTokensAsync(cancellationToken);
        
        // Cache for a very short time since this changes frequently
        await cacheService.SetAsync(ExpiredTokensKey, tokens, TimeSpan.FromMinutes(1), cancellationToken);
        logger.LogDebug("Cached expired tokens ({Count} tokens)", tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(PagedTokensKey, pageNumber, pageSize);
        
        var cachedTokens = await cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            logger.LogDebug("Cache hit for paged tokens (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return cachedTokens;
        }

        logger.LogDebug("Cache miss for paged tokens (page {PageNumber}, size {PageSize}), fetching from database", pageNumber, pageSize);
        var tokens = await repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached paged tokens (page {PageNumber}, size {PageSize}, {Count} tokens)", pageNumber, pageSize, tokens.Count);

        return tokens;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(refreshToken, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
        logger.LogDebug("Added refresh token and invalidated caches for user {UserId}", refreshToken.UserId.Value);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await repository.UpdateAsync(refreshToken, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
        await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
        logger.LogDebug("Updated refresh token and invalidated caches for token ID {TokenId}", refreshToken.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var token = await repository.GetByIdAsync(id, cancellationToken);
        
        await repository.DeleteAsync(id, cancellationToken);
        
        if (token is not null)
        {
            await InvalidateTokenCaches(token.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(token.Id, token.Token, cancellationToken);
            logger.LogDebug("Deleted refresh token and invalidated caches for token ID {TokenId}", id);
        }
    }

    public async Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var refreshToken = await repository.GetByTokenAsync(token, cancellationToken);
        
        await repository.DeleteByTokenAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
            logger.LogDebug("Deleted refresh token by token value and invalidated caches");
        }
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var refreshToken = await repository.GetByTokenAsync(token, cancellationToken);
        
        await repository.RevokeAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
            logger.LogDebug("Revoked refresh token and invalidated caches");
        }
    }

    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await repository.RevokeAllForUserAsync(userId, cancellationToken);
        
        // Invalidate all user-related token caches
        await InvalidateTokenCaches(userId, cancellationToken);
        logger.LogDebug("Revoked all refresh tokens and invalidated caches for user {UserId}", userId.Value);
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        await repository.DeleteExpiredTokensAsync(cancellationToken);
        
        // Invalidate all token-related caches since we don't know which users were affected
        await InvalidateAllTokenCaches(cancellationToken);
        logger.LogDebug("Deleted expired tokens and invalidated all token caches");
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenExistsKey, id);
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for token existence check ID {TokenId}", id);
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for token existence check ID {TokenId}, checking database", id);
        var exists = await repository.ExistsAsync(id, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached token existence check for ID {TokenId}: {Exists}", id, exists);

        return exists;
    }

    public async Task<bool> ExistsByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenExistsByTokenKey, GetTokenHash(token));
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for token existence check by token value");
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for token existence check by token value, checking database");
        var exists = await repository.ExistsByTokenAsync(token, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached token existence check by token value: {Exists}", exists);

        return exists;
    }

    public async Task<bool> IsValidTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenValidKey, GetTokenHash(token));
        
        var cachedValid = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedValid.HasValue)
        {
            logger.LogDebug("Cache hit for token validity check");
            return cachedValid.Value;
        }

        logger.LogDebug("Cache miss for token validity check, checking database");
        var isValid = await repository.IsValidTokenAsync(token, cancellationToken);
        
        // Cache for a shorter time since validity can change quickly
        await cacheService.SetAsync(cacheKey, isValid, TimeSpan.FromMinutes(2), cancellationToken);
        logger.LogDebug("Cached token validity check: {IsValid}", isValid);

        return isValid;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(TokenCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for token count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for token count, fetching from database");
        var count = await repository.GetCountAsync(cancellationToken);
        
        await cacheService.SetAsync(TokenCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached token count: {Count}", count);

        return count;
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(ActiveTokenCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for active token count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for active token count, fetching from database");
        var count = await repository.GetActiveCountAsync(cancellationToken);
        
        await cacheService.SetAsync(ActiveTokenCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached active token count: {Count}", count);

        return count;
    }

    public async Task<int> GetCountByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenCountByUserKey, userId.Value);
        
        var cachedCount = await cacheService.GetAsync<int?>(cacheKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for user token count {UserId}", userId.Value);
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for user token count {UserId}, fetching from database", userId.Value);
        var count = await repository.GetCountByUserAsync(userId, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached user token count for {UserId}: {Count}", userId.Value, count);

        return count;
    }

    private async Task InvalidateTokenCaches(UserId userId, CancellationToken cancellationToken)
    {
        // Invalidate user-specific caches
        await cacheService.RemoveAsync(string.Format(TokensByUserKey, userId.Value), cancellationToken);
        await cacheService.RemoveAsync(string.Format(ActiveTokensByUserKey, userId.Value), cancellationToken);
        await cacheService.RemoveAsync(string.Format(TokenCountByUserKey, userId.Value), cancellationToken);
        
        // Invalidate global caches
        await cacheService.RemoveAsync(ExpiredTokensKey, cancellationToken);
        await cacheService.RemoveAsync(TokenCountKey, cancellationToken);
        await cacheService.RemoveAsync(ActiveTokenCountKey, cancellationToken);
        
        // Invalidate paged caches using pattern
        await cacheService.RemoveByPatternAsync("tokens:paged:*", cancellationToken);
        
        // Invalidate using tags if supported
        await cacheService.RemoveByTagAsync(TokensTag, cancellationToken);
        await cacheService.RemoveByTagAsync(string.Format(UserTokensTag, userId.Value), cancellationToken);
    }

    private async Task InvalidateSpecificTokenCaches(Guid tokenId, string token, CancellationToken cancellationToken)
    {
        // Invalidate specific token caches
        await cacheService.RemoveAsync(string.Format(TokenByIdKey, tokenId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(TokenByTokenKey, GetTokenHash(token)), cancellationToken);
        await cacheService.RemoveAsync(string.Format(TokenExistsKey, tokenId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(TokenExistsByTokenKey, GetTokenHash(token)), cancellationToken);
        await cacheService.RemoveAsync(string.Format(TokenValidKey, GetTokenHash(token)), cancellationToken);
    }

    private async Task InvalidateAllTokenCaches(CancellationToken cancellationToken)
    {
        // Invalidate all token-related caches using patterns
        await cacheService.RemoveByPatternAsync("token:*", cancellationToken);
        await cacheService.RemoveByPatternAsync("tokens:*", cancellationToken);
        
        // Invalidate using tags if supported
        await cacheService.RemoveByTagAsync(TokensTag, cancellationToken);
    }

    private static string GetTokenHash(string token)
    {
        // Use a simple hash to avoid storing sensitive token values in cache keys
        return token.GetHashCode().ToString();
    }
}