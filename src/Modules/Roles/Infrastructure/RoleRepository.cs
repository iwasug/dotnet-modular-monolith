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
public sealed class RoleRepository(DbContext context, ILogger<RoleRepository> logger) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting role with ID {RoleId}", id.Value);
        
        return await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting role with name {RoleName}", name.Value);
        
        return await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all roles");
        
        return await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting active roles");
        
        return await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting paged roles - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
        
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Limit maximum page size
        
        return await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesWithPermissionAsync(PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting roles with permission {Resource}-{Action}-{Scope}", 
            permission.Resource, permission.Action, permission.Scope);
        
        return await context.Set<Role>()
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
        logger.LogDebug("Getting roles by IDs: {RoleIds}", string.Join(", ", ids));
        
        return await context.Set<Role>()
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

        logger.LogDebug("Adding role with ID {RoleId} and name {RoleName}", role.Id, role.Name.Value);
        
        await context.Set<Role>().AddAsync(role, cancellationToken);
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        if (role is null)
            throw new ArgumentNullException(nameof(role));

        logger.LogDebug("Updating role with ID {RoleId}", role.Id);
        
        role.UpdateTimestamp();
        context.Set<Role>().Update(role);
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting role with ID {RoleId}", id.Value);
        
        var role = await context.Set<Role>().FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
        if (role is not null)
        {
            context.Set<Role>().Remove(role);
        }
    }

    public async Task SoftDeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Soft deleting role with ID {RoleId}", id.Value);
        
        var role = await context.Set<Role>().FirstOrDefaultAsync(r => r.Id == id.Value, cancellationToken);
        if (role is not null)
        {
            role.SoftDelete();
            context.Set<Role>().Update(role);
        }
    }

    public async Task AddPermissionToRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Adding permission {Resource}-{Action}-{Scope} to role {RoleId}", 
            permission.Resource, permission.Action, permission.Scope, roleId.Value);
        
        var role = await context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.AddPermission(permission);
            role.UpdateTimestamp();
            context.Set<Role>().Update(role);
        }
    }

    public async Task RemovePermissionFromRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Removing permission {Resource}-{Action}-{Scope} from role {RoleId}", 
            permission.Resource, permission.Action, permission.Scope, roleId.Value);
        
        var role = await context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.RemovePermission(permission);
            role.UpdateTimestamp();
            context.Set<Role>().Update(role);
        }
    }

    public async Task SetRolePermissionsAsync(RoleId roleId, List<PermissionEntity> permissions, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Setting {PermissionCount} permissions for role {RoleId}", permissions.Count, roleId.Value);
        
        var role = await context.Set<Role>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        if (role is not null)
        {
            role.SetPermissions(permissions);
            role.UpdateTimestamp();
            context.Set<Role>().Update(role);
        }
    }

    public async Task<IReadOnlyList<PermissionEntity>> GetRolePermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting permissions for role {RoleId}", roleId.Value);
        
        var role = await context.Set<Role>()
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);
        
        return role?.Permissions.ToList() ?? new List<PermissionEntity>();
    }

    public async Task<bool> ExistsAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking existence of role with ID {RoleId}", id.Value);
        
        return await context.Set<Role>()
            .AsNoTracking()
            .AnyAsync(r => r.Id == id.Value, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking existence of role with name {RoleName}", name.Value);
        
        return await context.Set<Role>()
            .AsNoTracking()
            .AnyAsync(r => r.Name == name, cancellationToken);
    }

    public async Task<bool> RoleHasPermissionAsync(RoleId roleId, PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Checking if role {RoleId} has permission {Resource}-{Action}-{Scope}", 
            roleId.Value, permission.Resource, permission.Action, permission.Scope);
        
        return await context.Set<Role>()
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
        logger.LogDebug("Getting total role count");
        
        return await context.Set<Role>().CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting active role count");
        
        return await context.Set<Role>()
            .Where(r => !r.IsDeleted)
            .CountAsync(cancellationToken);
    }
}