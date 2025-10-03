using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Infrastructure;

/// <summary>
/// Cached refresh token repository implementation using cache-aside pattern
/// </summary>
public sealed class CachedRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IRefreshTokenRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedRefreshTokenRepository> _logger;

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

    public CachedRefreshTokenRepository(
        IRefreshTokenRepository repository,
        ICacheService cacheService,
        ILogger<CachedRefreshTokenRepository> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenByIdKey, id);
        
        var cachedToken = await _cacheService.GetAsync<RefreshToken>(cacheKey, cancellationToken);
        if (cachedToken is not null)
        {
            _logger.LogDebug("Cache hit for refresh token ID {TokenId}", id);
            return cachedToken;
        }

        _logger.LogDebug("Cache miss for refresh token ID {TokenId}, fetching from database", id);
        var token = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (token is not null)
        {
            await _cacheService.SetAsync(cacheKey, token, DefaultExpiration, cancellationToken);
            _logger.LogDebug("Cached refresh token with ID {TokenId}", id);
        }

        return token;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenByTokenKey, GetTokenHash(token));
        
        var cachedToken = await _cacheService.GetAsync<RefreshToken>(cacheKey, cancellationToken);
        if (cachedToken is not null)
        {
            _logger.LogDebug("Cache hit for refresh token by token value");
            return cachedToken;
        }

        _logger.LogDebug("Cache miss for refresh token by token value, fetching from database");
        var refreshToken = await _repository.GetByTokenAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await _cacheService.SetAsync(cacheKey, refreshToken, DefaultExpiration, cancellationToken);
            // Also cache by ID for consistency
            var idCacheKey = string.Format(TokenByIdKey, refreshToken.Id);
            await _cacheService.SetAsync(idCacheKey, refreshToken, DefaultExpiration, cancellationToken);
            _logger.LogDebug("Cached refresh token by token value");
        }

        return refreshToken;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokensByUserKey, userId.Value);
        
        var cachedTokens = await _cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            _logger.LogDebug("Cache hit for user tokens {UserId}", userId.Value);
            return cachedTokens;
        }

        _logger.LogDebug("Cache miss for user tokens {UserId}, fetching from database", userId.Value);
        var tokens = await _repository.GetByUserIdAsync(userId, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        _logger.LogDebug("Cached user tokens for {UserId} ({Count} tokens)", userId.Value, tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(ActiveTokensByUserKey, userId.Value);
        
        var cachedTokens = await _cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            _logger.LogDebug("Cache hit for active user tokens {UserId}", userId.Value);
            return cachedTokens;
        }

        _logger.LogDebug("Cache miss for active user tokens {UserId}, fetching from database", userId.Value);
        var tokens = await _repository.GetActiveByUserIdAsync(userId, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        _logger.LogDebug("Cached active user tokens for {UserId} ({Count} tokens)", userId.Value, tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var cachedTokens = await _cacheService.GetAsync<IReadOnlyList<RefreshToken>>(ExpiredTokensKey, cancellationToken);
        if (cachedTokens is not null)
        {
            _logger.LogDebug("Cache hit for expired tokens");
            return cachedTokens;
        }

        _logger.LogDebug("Cache miss for expired tokens, fetching from database");
        var tokens = await _repository.GetExpiredTokensAsync(cancellationToken);
        
        // Cache for a very short time since this changes frequently
        await _cacheService.SetAsync(ExpiredTokensKey, tokens, TimeSpan.FromMinutes(1), cancellationToken);
        _logger.LogDebug("Cached expired tokens ({Count} tokens)", tokens.Count);

        return tokens;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(PagedTokensKey, pageNumber, pageSize);
        
        var cachedTokens = await _cacheService.GetAsync<IReadOnlyList<RefreshToken>>(cacheKey, cancellationToken);
        if (cachedTokens is not null)
        {
            _logger.LogDebug("Cache hit for paged tokens (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return cachedTokens;
        }

        _logger.LogDebug("Cache miss for paged tokens (page {PageNumber}, size {PageSize}), fetching from database", pageNumber, pageSize);
        var tokens = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, tokens, ShortExpiration, cancellationToken);
        _logger.LogDebug("Cached paged tokens (page {PageNumber}, size {PageSize}, {Count} tokens)", pageNumber, pageSize, tokens.Count);

        return tokens;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(refreshToken, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
        _logger.LogDebug("Added refresh token and invalidated caches for user {UserId}", refreshToken.UserId.Value);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(refreshToken, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
        await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
        _logger.LogDebug("Updated refresh token and invalidated caches for token ID {TokenId}", refreshToken.Id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var token = await _repository.GetByIdAsync(id, cancellationToken);
        
        await _repository.DeleteAsync(id, cancellationToken);
        
        if (token is not null)
        {
            await InvalidateTokenCaches(token.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(token.Id, token.Token, cancellationToken);
            _logger.LogDebug("Deleted refresh token and invalidated caches for token ID {TokenId}", id);
        }
    }

    public async Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var refreshToken = await _repository.GetByTokenAsync(token, cancellationToken);
        
        await _repository.DeleteByTokenAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
            _logger.LogDebug("Deleted refresh token by token value and invalidated caches");
        }
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        // Get token first to get user ID for cache invalidation
        var refreshToken = await _repository.GetByTokenAsync(token, cancellationToken);
        
        await _repository.RevokeAsync(token, cancellationToken);
        
        if (refreshToken is not null)
        {
            await InvalidateTokenCaches(refreshToken.UserId, cancellationToken);
            await InvalidateSpecificTokenCaches(refreshToken.Id, refreshToken.Token, cancellationToken);
            _logger.LogDebug("Revoked refresh token and invalidated caches");
        }
    }

    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await _repository.RevokeAllForUserAsync(userId, cancellationToken);
        
        // Invalidate all user-related token caches
        await InvalidateTokenCaches(userId, cancellationToken);
        _logger.LogDebug("Revoked all refresh tokens and invalidated caches for user {UserId}", userId.Value);
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        await _repository.DeleteExpiredTokensAsync(cancellationToken);
        
        // Invalidate all token-related caches since we don't know which users were affected
        await InvalidateAllTokenCaches(cancellationToken);
        _logger.LogDebug("Deleted expired tokens and invalidated all token caches");
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenExistsKey, id);
        
        var cachedExists = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            _logger.LogDebug("Cache hit for token existence check ID {TokenId}", id);
            return cachedExists.Value;
        }

        _logger.LogDebug("Cache miss for token existence check ID {TokenId}, checking database", id);
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        _logger.LogDebug("Cached token existence check for ID {TokenId}: {Exists}", id, exists);

        return exists;
    }

    public async Task<bool> ExistsByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenExistsByTokenKey, GetTokenHash(token));
        
        var cachedExists = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            _logger.LogDebug("Cache hit for token existence check by token value");
            return cachedExists.Value;
        }

        _logger.LogDebug("Cache miss for token existence check by token value, checking database");
        var exists = await _repository.ExistsByTokenAsync(token, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        _logger.LogDebug("Cached token existence check by token value: {Exists}", exists);

        return exists;
    }

    public async Task<bool> IsValidTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenValidKey, GetTokenHash(token));
        
        var cachedValid = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedValid.HasValue)
        {
            _logger.LogDebug("Cache hit for token validity check");
            return cachedValid.Value;
        }

        _logger.LogDebug("Cache miss for token validity check, checking database");
        var isValid = await _repository.IsValidTokenAsync(token, cancellationToken);
        
        // Cache for a shorter time since validity can change quickly
        await _cacheService.SetAsync(cacheKey, isValid, TimeSpan.FromMinutes(2), cancellationToken);
        _logger.LogDebug("Cached token validity check: {IsValid}", isValid);

        return isValid;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await _cacheService.GetAsync<int?>(TokenCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            _logger.LogDebug("Cache hit for token count");
            return cachedCount.Value;
        }

        _logger.LogDebug("Cache miss for token count, fetching from database");
        var count = await _repository.GetCountAsync(cancellationToken);
        
        await _cacheService.SetAsync(TokenCountKey, count, CountExpiration, cancellationToken);
        _logger.LogDebug("Cached token count: {Count}", count);

        return count;
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await _cacheService.GetAsync<int?>(ActiveTokenCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            _logger.LogDebug("Cache hit for active token count");
            return cachedCount.Value;
        }

        _logger.LogDebug("Cache miss for active token count, fetching from database");
        var count = await _repository.GetActiveCountAsync(cancellationToken);
        
        await _cacheService.SetAsync(ActiveTokenCountKey, count, CountExpiration, cancellationToken);
        _logger.LogDebug("Cached active token count: {Count}", count);

        return count;
    }

    public async Task<int> GetCountByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(TokenCountByUserKey, userId.Value);
        
        var cachedCount = await _cacheService.GetAsync<int?>(cacheKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            _logger.LogDebug("Cache hit for user token count {UserId}", userId.Value);
            return cachedCount.Value;
        }

        _logger.LogDebug("Cache miss for user token count {UserId}, fetching from database", userId.Value);
        var count = await _repository.GetCountByUserAsync(userId, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, count, CountExpiration, cancellationToken);
        _logger.LogDebug("Cached user token count for {UserId}: {Count}", userId.Value, count);

        return count;
    }

    private async Task InvalidateTokenCaches(UserId userId, CancellationToken cancellationToken)
    {
        // Invalidate user-specific caches
        await _cacheService.RemoveAsync(string.Format(TokensByUserKey, userId.Value), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(ActiveTokensByUserKey, userId.Value), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(TokenCountByUserKey, userId.Value), cancellationToken);
        
        // Invalidate global caches
        await _cacheService.RemoveAsync(ExpiredTokensKey, cancellationToken);
        await _cacheService.RemoveAsync(TokenCountKey, cancellationToken);
        await _cacheService.RemoveAsync(ActiveTokenCountKey, cancellationToken);
        
        // Invalidate paged caches using pattern
        await _cacheService.RemoveByPatternAsync("tokens:paged:*", cancellationToken);
        
        // Invalidate using tags if supported
        await _cacheService.RemoveByTagAsync(TokensTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(string.Format(UserTokensTag, userId.Value), cancellationToken);
    }

    private async Task InvalidateSpecificTokenCaches(Guid tokenId, string token, CancellationToken cancellationToken)
    {
        // Invalidate specific token caches
        await _cacheService.RemoveAsync(string.Format(TokenByIdKey, tokenId), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(TokenByTokenKey, GetTokenHash(token)), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(TokenExistsKey, tokenId), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(TokenExistsByTokenKey, GetTokenHash(token)), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(TokenValidKey, GetTokenHash(token)), cancellationToken);
    }

    private async Task InvalidateAllTokenCaches(CancellationToken cancellationToken)
    {
        // Invalidate all token-related caches using patterns
        await _cacheService.RemoveByPatternAsync("token:*", cancellationToken);
        await _cacheService.RemoveByPatternAsync("tokens:*", cancellationToken);
        
        // Invalidate using tags if supported
        await _cacheService.RemoveByTagAsync(TokensTag, cancellationToken);
    }

    private static string GetTokenHash(string token)
    {
        // Use a simple hash to avoid storing sensitive token values in cache keys
        return token.GetHashCode().ToString();
    }
}