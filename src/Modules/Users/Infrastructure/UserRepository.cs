using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Infrastructure;

/// <summary>
/// User repository implementation with user-specific queries and operations
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly DbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(DbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user with ID {UserId}", id.Value);
        
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user with email {Email}", email.Value);
        
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all users");
        
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active users");
        
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged users - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Limit maximum page size
        
        return await _context.Set<User>()
            .AsNoTracking()
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        _logger.LogDebug("Adding user with ID {UserId} and email {Email}", user.Id, user.Email.Value);
        
        await _context.Set<User>().AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        _logger.LogDebug("Updating user with ID {UserId}", user.Id);
        
        user.UpdateTimestamp();
        _context.Set<User>().Update(user);
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting user with ID {UserId}", id.Value);
        
        var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
        if (user is not null)
        {
            _context.Set<User>().Remove(user);
        }
    }

    public async Task SoftDeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Soft deleting user with ID {UserId}", id.Value);
        
        var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == id.Value, cancellationToken);
        if (user is not null)
        {
            user.SoftDelete();
            _context.Set<User>().Update(user);
        }
    }

    public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of user with ID {UserId}", id.Value);
        
        return await _context.Set<User>()
            .AsNoTracking()
            .AnyAsync(u => u.Id == id.Value, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of user with email {Email}", email.Value);
        
        return await _context.Set<User>()
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting total user count");
        
        return await _context.Set<User>().CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active user count");
        
        return await _context.Set<User>()
            .Where(u => !u.IsDeleted)
            .CountAsync(cancellationToken);
    }
}