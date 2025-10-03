using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Infrastructure;

/// <summary>
/// Cached user repository implementation using cache-aside pattern
/// </summary>
public sealed class CachedUserRepository : IUserRepository
{
    private readonly IUserRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserRepository> _logger;

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

    public CachedUserRepository(
        IUserRepository repository,
        ICacheService cacheService,
        ILogger<CachedUserRepository> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserByIdKey, id.Value);
        
        var cachedUser = await _cacheService.GetAsync<User>(cacheKey, cancellationToken);
        if (cachedUser is not null)
        {
            _logger.LogDebug("Cache hit for user ID {UserId}", id.Value);
            return cachedUser;
        }

        _logger.LogDebug("Cache miss for user ID {UserId}, fetching from database", id.Value);
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await _cacheService.SetAsync(cacheKey, user, DefaultExpiration, cancellationToken);
            _logger.LogDebug("Cached user with ID {UserId}", id.Value);
        }

        return user;
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserByEmailKey, email.Value);
        
        var cachedUser = await _cacheService.GetAsync<User>(cacheKey, cancellationToken);
        if (cachedUser is not null)
        {
            _logger.LogDebug("Cache hit for user email {Email}", email.Value);
            return cachedUser;
        }

        _logger.LogDebug("Cache miss for user email {Email}, fetching from database", email.Value);
        var user = await _repository.GetByEmailAsync(email, cancellationToken);
        
        if (user is not null)
        {
            await _cacheService.SetAsync(cacheKey, user, DefaultExpiration, cancellationToken);
            // Also cache by ID for consistency
            var idCacheKey = string.Format(UserByIdKey, user.Id);
            await _cacheService.SetAsync(idCacheKey, user, DefaultExpiration, cancellationToken);
            _logger.LogDebug("Cached user with email {Email}", email.Value);
        }

        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedUsers = await _cacheService.GetAsync<IReadOnlyList<User>>(AllUsersKey, cancellationToken);
        if (cachedUsers is not null)
        {
            _logger.LogDebug("Cache hit for all users");
            return cachedUsers;
        }

        _logger.LogDebug("Cache miss for all users, fetching from database");
        var users = await _repository.GetAllAsync(cancellationToken);
        
        await _cacheService.SetAsync(AllUsersKey, users, ShortExpiration, cancellationToken);
        _logger.LogDebug("Cached all users ({Count} users)", users.Count);

        return users;
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var cachedUsers = await _cacheService.GetAsync<IReadOnlyList<User>>(ActiveUsersKey, cancellationToken);
        if (cachedUsers is not null)
        {
            _logger.LogDebug("Cache hit for active users");
            return cachedUsers;
        }

        _logger.LogDebug("Cache miss for active users, fetching from database");
        var users = await _repository.GetActiveUsersAsync(cancellationToken);
        
        await _cacheService.SetAsync(ActiveUsersKey, users, DefaultExpiration, cancellationToken);
        _logger.LogDebug("Cached active users ({Count} users)", users.Count);

        return users;
    }

    public async Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(PagedUsersKey, pageNumber, pageSize);
        
        var cachedUsers = await _cacheService.GetAsync<IReadOnlyList<User>>(cacheKey, cancellationToken);
        if (cachedUsers is not null)
        {
            _logger.LogDebug("Cache hit for paged users (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return cachedUsers;
        }

        _logger.LogDebug("Cache miss for paged users (page {PageNumber}, size {PageSize}), fetching from database", pageNumber, pageSize);
        var users = await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, users, ShortExpiration, cancellationToken);
        _logger.LogDebug("Cached paged users (page {PageNumber}, size {PageSize}, {Count} users)", pageNumber, pageSize, users.Count);

        return users;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(user, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
        _logger.LogDebug("Added user and invalidated caches for user ID {UserId}", user.Id);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(user, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
        _logger.LogDebug("Updated user and invalidated caches for user ID {UserId}", user.Id);
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // Get user first to get email for cache invalidation
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        
        await _repository.DeleteAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
            _logger.LogDebug("Deleted user and invalidated caches for user ID {UserId}", id.Value);
        }
    }

    public async Task SoftDeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        // Get user first to get email for cache invalidation
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        
        await _repository.SoftDeleteAsync(id, cancellationToken);
        
        if (user is not null)
        {
            await InvalidateUserCaches(user.Id, user.Email.Value, cancellationToken);
            _logger.LogDebug("Soft deleted user and invalidated caches for user ID {UserId}", id.Value);
        }
    }

    public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserExistsKey, id.Value);
        
        var cachedExists = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            _logger.LogDebug("Cache hit for user existence check ID {UserId}", id.Value);
            return cachedExists.Value;
        }

        _logger.LogDebug("Cache miss for user existence check ID {UserId}, checking database", id.Value);
        var exists = await _repository.ExistsAsync(id, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        _logger.LogDebug("Cached user existence check for ID {UserId}: {Exists}", id.Value, exists);

        return exists;
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(UserExistsByEmailKey, email.Value);
        
        var cachedExists = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            _logger.LogDebug("Cache hit for user existence check email {Email}", email.Value);
            return cachedExists.Value;
        }

        _logger.LogDebug("Cache miss for user existence check email {Email}, checking database", email.Value);
        var exists = await _repository.ExistsByEmailAsync(email, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        _logger.LogDebug("Cached user existence check for email {Email}: {Exists}", email.Value, exists);

        return exists;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await _cacheService.GetAsync<int?>(UserCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            _logger.LogDebug("Cache hit for user count");
            return cachedCount.Value;
        }

        _logger.LogDebug("Cache miss for user count, fetching from database");
        var count = await _repository.GetCountAsync(cancellationToken);
        
        await _cacheService.SetAsync(UserCountKey, count, CountExpiration, cancellationToken);
        _logger.LogDebug("Cached user count: {Count}", count);

        return count;
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await _cacheService.GetAsync<int?>(ActiveUserCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            _logger.LogDebug("Cache hit for active user count");
            return cachedCount.Value;
        }

        _logger.LogDebug("Cache miss for active user count, fetching from database");
        var count = await _repository.GetActiveCountAsync(cancellationToken);
        
        await _cacheService.SetAsync(ActiveUserCountKey, count, CountExpiration, cancellationToken);
        _logger.LogDebug("Cached active user count: {Count}", count);

        return count;
    }

    private async Task InvalidateUserCaches(Guid userId, string email, CancellationToken cancellationToken)
    {
        // Invalidate specific user caches
        await _cacheService.RemoveAsync(string.Format(UserByIdKey, userId), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(UserByEmailKey, email), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(UserExistsKey, userId), cancellationToken);
        await _cacheService.RemoveAsync(string.Format(UserExistsByEmailKey, email), cancellationToken);
        
        // Invalidate list caches
        await _cacheService.RemoveAsync(AllUsersKey, cancellationToken);
        await _cacheService.RemoveAsync(ActiveUsersKey, cancellationToken);
        await _cacheService.RemoveAsync(UserCountKey, cancellationToken);
        await _cacheService.RemoveAsync(ActiveUserCountKey, cancellationToken);
        
        // Invalidate paged caches using pattern
        await _cacheService.RemoveByPatternAsync("users:paged:*", cancellationToken);
        
        // Invalidate using tags if supported
        await _cacheService.RemoveByTagAsync(UsersTag, cancellationToken);
        await _cacheService.RemoveByTagAsync(string.Format(UserTag, userId), cancellationToken);
    }
}