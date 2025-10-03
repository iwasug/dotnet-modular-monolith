using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using PermissionEntity = ModularMonolith.Shared.Domain.Permission;
using PermissionValueObject = ModularMonolith.Roles.Domain.ValueObjects.Permission;

namespace ModularMonolith.Roles.Infrastructure;

/// <summary>
/// Role repository implementation with role-specific queries and permission management
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly DbContext _context;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(DbContext context, ILogger<RoleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting role with ID {RoleId}", id.Value);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting role with name {RoleName}", name.Value);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all roles");
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active roles");
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged roles - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Limit maximum page size
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesWithPermissionAsync(PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting roles with permission {Resource}-{Action}-{Scope}", 
            permission.Resource, permission.Action, permission.Scope);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => r.Permissions.Any(p => 
                p.Resource == permission.Resource && 
                p.Action == permission.Action && 
                p.Scope == permission.Scope))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<RoleId> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Select(id => id.Value).ToList();
        _logger.LogDebug("Getting roles by IDs: {RoleIds}", string.Join(", ", ids));
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => ids.Contains(r.Id))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        _logger.LogDebug("Adding role with ID {RoleId} and name {RoleName}", role.Id, role.Name.Value);
        
        await _context.Set<Role>().AddAsync(role, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        _logger.LogDebug("Updating role with ID {RoleId}", role.Id);
        
        role.UpdateTimestamp();
        _context.Set<Role>().Update(role);
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting role with ID {RoleId}", id.Value);
        
        var role = await _context.Set<Role>().FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
        if (role is not null)
        {
            _context.Set<Role>().Remove(role);
        }
    }

    public async Task SoftDeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Soft deleting role with ID {RoleId}", id.Value);
        
        var role = await _context.Set<Role>().FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
        if (role is not null)
        {
            role.SoftDelete();
            _context.Set<Role>().Update(role);
        }
    }

    public async Task AddPermissionToRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding permission {Resource}-{Action}-{Scope} to role {RoleId}", 
            permission.Resource, permission.Action, permission.Scope, roleId.Value);
        
        var role = await _context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.AddPermission(permission);
            role.UpdateTimestamp();
            _context.Set<Role>().Update(role);
        }
    }

    public async Task RemovePermissionFromRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing permission {Resource}-{Action}-{Scope} from role {RoleId}", 
            permission.Resource, permission.Action, permission.Scope, roleId.Value);
        
        var role = await _context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.RemovePermission(permission);
            role.UpdateTimestamp();
            _context.Set<Role>().Update(role);
        }
    }

    public async Task SetRolePermissionsAsync(RoleId roleId, List<PermissionEntity> permissions, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting {PermissionCount} permissions for role {RoleId}", permissions.Count, roleId.Value);
        
        var role = await _context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.SetPermissions(permissions);
            role.UpdateTimestamp();
            _context.Set<Role>().Update(role);
        }
    }

    public async Task<IReadOnlyList<PermissionEntity>> GetRolePermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting permissions for role {RoleId}", roleId.Value);
        
        var role = await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        return role?.Permissions.ToList() ?? new List<PermissionEntity>();
    }

    public async Task<bool> ExistsAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of role with ID {RoleId}", id.Value);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .AnyAsync(r => r.Id == id.Value, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of role with name {RoleName}", name.Value);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .AnyAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<bool> RoleHasPermissionAsync(RoleId roleId, PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if role {RoleId} has permission {Resource}-{Action}-{Scope}", 
            roleId.Value, permission.Resource, permission.Action, permission.Scope);
        
        return await _context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => r.Id == roleId.Value)
            .AnyAsync(r => r.Permissions.Any(p => 
                p.Resource == permission.Resource && 
                p.Action == permission.Action && 
                p.Scope == permission.Scope), cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting total role count");
        
        return await _context.Set<Role>().CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active role count");
        
        return await _context.Set<Role>()
            .Where(r => !r.IsDeleted)
            .CountAsync(cancellationToken);
    }
}