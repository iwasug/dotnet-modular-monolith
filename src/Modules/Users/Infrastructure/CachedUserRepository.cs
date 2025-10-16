using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Infrastructure;

/// <summary>
/// Cached user repository implementation using cache-aside pattern
/// </summary>
public sealed class CachedUserRepository(
    IUserRepository repository,
    ICacheService cacheService,
    ILogger<CachedUserRepository> logger)
    : IUserRepository
{
    // Cache key patterns
    private const string UserByIdKey = "user:id:{0}";
    private const string UserByEmailKey = "user:email:{0}";
    private const string AllUsersKey = "users:all";
    private const string ActiveUsersKey = "users:active";
    private const string PagedUsersKey = "users:paged:{0}:{1}";
    private const string UserCountKey = "users:count";
    private const string ActiveUserCountKey = "users:count:active";
    private const string UserExistsKey = "user:exists:id:{0}";
    private const string UserExistsByEmailKey = "user:exists:email:{0}";

    // Cache tags for invalidation
    private const string UsersTag = "users";
    private const string UserTag = "user:{0}";

    // Cache expiration times
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CountExpiration = TimeSpan.FromMinutes(30);

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserByIdKey, id.Value);
        
        var cachedUser = await cacheService.GetAsync<User>(cacheKey, cancellationToken);
        if (cachedUser is not null)
        {
            logger.LogDebug("Cache hit for user ID {UserId}", id.Value);
            return cachedUser;
        }

        logger.LogDebug("Cache miss for user ID {UserId}, fetching from database", id.Value);
        var user = await repository.GetByIdAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await cacheService.SetAsync(cacheKey, user, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached user with ID {UserId}", id.Value);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserByEmailKey, email.Value);
        
        var cachedUser = await cacheService.GetAsync<User>(cacheKey, cancellationToken);
        if (cachedUser is not null)
        {
            logger.LogDebug("Cache hit for user email {Email}", email.Value);
            return cachedUser;
        }

        logger.LogDebug("Cache miss for user email {Email}, fetching from database", email.Value);
        var user = await repository.GetByEmailAsync(email, cancellationToken);
        
        if (user is not null)
        {
            await cacheService.SetAsync(cacheKey, user, DefaultExpiration, cancellationToken);
            // Also cache by ID for consistency
            var idCacheKey = string.Format(UserByIdKey, user.Id);
            await cacheService.SetAsync(idCacheKey, user, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached user with email {Email}", email.Value);
        }

        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedUsers = await cacheService.GetAsync<IReadOnlyList<User>>(AllUsersKey, cancellationToken);
        if (cachedUsers is not null)
        {
            logger.LogDebug("Cache hit for all users");
            return cachedUsers;
        }

        logger.LogDebug("Cache miss for all users, fetching from database");
        var users = await repository.GetAllAsync(cancellationToken);
        
        await cacheService.SetAsync(AllUsersKey, users, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached all users ({Count} users)", users.Count);

        return users;
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var cachedUsers = await cacheService.GetAsync<IReadOnlyList<User>>(ActiveUsersKey, cancellationToken);
        if (cachedUsers is not null)
        {
            logger.LogDebug("Cache hit for active users");
            return cachedUsers;
        }

        logger.LogDebug("Cache miss for active users, fetching from database");
        var users = await repository.GetActiveUsersAsync(cancellationToken);
        
        await cacheService.SetAsync(ActiveUsersKey, users, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached active users ({Count} users)", users.Count);

        return users;
    }

    public async Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(PagedUsersKey, pageNumber, pageSize);
        
        var cachedUsers = await cacheService.GetAsync<IReadOnlyList<User>>(cacheKey, cancellationToken);
        if (cachedUsers is not null)
        {
            logger.LogDebug("Cache hit for paged users (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return cachedUsers;
        }

        logger.LogDebug("Cache miss for paged users (page {PageNumber}, size {PageSize}), fetching from database", pageNumber, pageSize);
        var users = await repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, users, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached paged users (page {PageNumber}, size {PageSize}, {Count} users)", pageNumber, pageSize, users.Count);

        return users;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(user, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
        logger.LogDebug("Added user and invalidated caches for user ID {UserId}", user.Id);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await repository.UpdateAsync(user, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
        logger.LogDebug("Updated user and invalidated caches for user ID {UserId}", user.Id);
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // Get user first to get email for cache invalidation
        var user = await repository.GetByIdAsync(id, cancellationToken);
        
        await repository.DeleteAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
            logger.LogDebug("Deleted user and invalidated caches for user ID {UserId}", id.Value);
        }
    }

    public async Task SoftDeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // Get user first to get email for cache invalidation
        var user = await repository.GetByIdAsync(id, cancellationToken);
        
        await repository.SoftDeleteAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
            logger.LogDebug("Soft deleted user and invalidated caches for user ID {UserId}", id.Value);
        }
    }

    public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserExistsKey, id.Value);
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for user existence check ID {UserId}", id.Value);
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for user existence check ID {UserId}, checking database", id.Value);
        var exists = await repository.ExistsAsync(id, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached user existence check for ID {UserId}: {Exists}", id.Value, exists);

        return exists;
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserExistsByEmailKey, email.Value);
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for user existence check email {Email}", email.Value);
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for user existence check email {Email}, checking database", email.Value);
        var exists = await repository.ExistsByEmailAsync(email, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached user existence check for email {Email}: {Exists}", email.Value, exists);

        return exists;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(UserCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for user count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for user count, fetching from database");
        var count = await repository.GetCountAsync(cancellationToken);
        
        await cacheService.SetAsync(UserCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached user count: {Count}", count);

        return count;
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(ActiveUserCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for active user count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for active user count, fetching from database");
        var count = await repository.GetActiveCountAsync(cancellationToken);
        
        await cacheService.SetAsync(ActiveUserCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached active user count: {Count}", count);

        return count;
    }

    private async Task InvalidateUserCaches(Guid userId, string email, CancellationToken cancellationToken)
    {
        // Invalidate specific user caches
        await cacheService.RemoveAsync(string.Format(UserByIdKey, userId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(UserByEmailKey, email), cancellationToken);
        await cacheService.RemoveAsync(string.Format(UserExistsKey, userId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(UserExistsByEmailKey, email), cancellationToken);
        
        // Invalidate list caches
        await cacheService.RemoveAsync(AllUsersKey, cancellationToken);
        await cacheService.RemoveAsync(ActiveUsersKey, cancellationToken);
        await cacheService.RemoveAsync(UserCountKey, cancellationToken);
        await cacheService.RemoveAsync(ActiveUserCountKey, cancellationToken);
        
        // Invalidate paged caches using pattern
        await cacheService.RemoveByPatternAsync("users:paged:*", cancellationToken);
        
        // Invalidate using tags if supported
        await cacheService.RemoveByTagAsync(UsersTag, cancellationToken);
        await cacheService.RemoveByTagAsync(string.Format(UserTag, userId), cancellationToken);
    }
}