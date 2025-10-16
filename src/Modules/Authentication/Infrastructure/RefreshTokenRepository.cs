using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Authentication.Infrastructure;

/// <summary>
/// Refresh token repository implementation with token-specific operations and cleanup functionality
/// </summary>
public sealed class RefreshTokenRepository(DbContext context, ILogger<RefreshTokenRepository> logger)
    : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting refresh token with ID {TokenId}", id);
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting refresh token by token value");
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all refresh tokens for user {UserId}", userId.Value);
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting active refresh tokens for user {UserId}", userId.Value);
        
        var now = DateTime.UtcNow;
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting expired refresh tokens");
        
        var now = DateTime.UtcNow;
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .Where(rt => rt.ExpiresAt <= now)
            .OrderBy(rt => rt.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting paged refresh tokens - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Limit maximum page size
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .OrderByDescending(rt => rt.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken is null)
            throw new ArgumentNullException(nameof(refreshToken));

        logger.LogDebug("Adding refresh token with ID {TokenId} for user {UserId}", 
            refreshToken.Id, refreshToken.UserId.Value);
        
        await context.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        if (refreshToken is null)
            throw new ArgumentNullException(nameof(refreshToken));

        logger.LogDebug("Updating refresh token with ID {TokenId}", refreshToken.Id);
        
        refreshToken.UpdateTimestamp();
        context.Set<RefreshToken>().Update(refreshToken);
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting refresh token with ID {TokenId}", id);
        
        var refreshToken = await context.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
        if (refreshToken is not null)
        {
            context.Set<RefreshToken>().Remove(refreshToken);
        }
    }

    public async Task DeleteByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting refresh token by token value");
        
        var refreshToken = await context.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        if (refreshToken is not null)
        {
            context.Set<RefreshToken>().Remove(refreshToken);
        }
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Revoking refresh token by token value");
        
        var refreshToken = await context.Set<RefreshToken>().FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        if (refreshToken is not null && !refreshToken.IsRevoked)
        {
            refreshToken.Revoke();
            context.Set<RefreshToken>().Update(refreshToken);
        }
    }

    public async Task RevokeAllForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Revoking all refresh tokens for user {UserId}", userId.Value);
        
        var activeTokens = await context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);
        
        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
        
        if (activeTokens.Count > 0)
        {
            context.Set<RefreshToken>().UpdateRange(activeTokens);
        }
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting expired refresh tokens");
        
        var now = DateTime.UtcNow;
        var expiredTokens = await context.Set<RefreshToken>()
            .Where(rt => rt.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        
        if (expiredTokens.Count > 0)
        {
            logger.LogInformation("Deleting {ExpiredTokenCount} expired refresh tokens", expiredTokens.Count);
            context.Set<RefreshToken>().RemoveRange(expiredTokens);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking existence of refresh token with ID {TokenId}", id);
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .AnyAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking existence of refresh token by token value");
        
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .AnyAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<bool> IsValidTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking validity of refresh token");
        
        var now = DateTime.UtcNow;
        return await context.Set<RefreshToken>()
            .AsNoTracking()
            .AnyAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > now, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting total refresh token count");
        
        return await context.Set<RefreshToken>().CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting active refresh token count");
        
        var now = DateTime.UtcNow;
        return await context.Set<RefreshToken>()
            .Where(rt => !rt.IsRevoked && rt.ExpiresAt > now)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting refresh token count for user {UserId}", userId.Value);
        
        return await context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId)
            .CountAsync(cancellationToken);
    }
}