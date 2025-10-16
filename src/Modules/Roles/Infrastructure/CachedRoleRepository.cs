using Microsoft.Extensions.Logging;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using PermissionEntity = ModularMonolith.Shared.Domain.Permission;
using PermissionValueObject = ModularMonolith.Roles.Domain.ValueObjects.Permission;

namespace ModularMonolith.Roles.Infrastructure;

/// <summary>
/// Cached role repository implementation using cache-aside pattern
/// </summary>
public sealed class CachedRoleRepository(
    IRoleRepository repository,
    ICacheService cacheService,
    ILogger<CachedRoleRepository> logger)
    : IRoleRepository
{
    // Cache key patterns
    private const string RoleByIdKey = "role:id:{0}";
    private const string RoleByNameKey = "role:name:{0}";
    private const string AllRolesKey = "roles:all";
    private const string ActiveRolesKey = "roles:active";
    private const string PagedRolesKey = "roles:paged:{0}:{1}";
    private const string RolesByIdsKey = "roles:ids:{0}";
    private const string RolesWithPermissionKey = "roles:permission:{0}:{1}:{2}";
    private const string RolePermissionsKey = "role:permissions:{0}";
    private const string RoleCountKey = "roles:count";
    private const string ActiveRoleCountKey = "roles:count:active";
    private const string RoleExistsKey = "role:exists:id:{0}";
    private const string RoleExistsByNameKey = "role:exists:name:{0}";
    private const string RoleHasPermissionKey = "role:haspermission:{0}:{1}:{2}:{3}";

    // Cache tags for invalidation
    private const string RolesTag = "roles";
    private const string RoleTag = "role:{0}";

    // Cache expiration times
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ShortExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CountExpiration = TimeSpan.FromMinutes(30);

    public async Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RoleByIdKey, id.Value);
        
        var cachedRole = await cacheService.GetAsync<Role>(cacheKey, cancellationToken);
        if (cachedRole is not null)
        {
            logger.LogDebug("Cache hit for role ID {RoleId}", id.Value);
            return cachedRole;
        }

        logger.LogDebug("Cache miss for role ID {RoleId}, fetching from database", id.Value);
        var role = await repository.GetByIdAsync(id, cancellationToken);
        
        if (role is not null)
        {
            await cacheService.SetAsync(cacheKey, role, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached role with ID {RoleId}", id.Value);
        }

        return role;
    }

    public async Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RoleByNameKey, name.Value);
        
        var cachedRole = await cacheService.GetAsync<Role>(cacheKey, cancellationToken);
        if (cachedRole is not null)
        {
            logger.LogDebug("Cache hit for role name {RoleName}", name.Value);
            return cachedRole;
        }

        logger.LogDebug("Cache miss for role name {RoleName}, fetching from database", name.Value);
        var role = await repository.GetByNameAsync(name, cancellationToken);
        
        if (role is not null)
        {
            await cacheService.SetAsync(cacheKey, role, DefaultExpiration, cancellationToken);
            // Also cache by ID for consistency
            var idCacheKey = string.Format(RoleByIdKey, role.Id);
            await cacheService.SetAsync(idCacheKey, role, DefaultExpiration, cancellationToken);
            logger.LogDebug("Cached role with name {RoleName}", name.Value);
        }

        return role;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedRoles = await cacheService.GetAsync<IReadOnlyList<Role>>(AllRolesKey, cancellationToken);
        if (cachedRoles is not null)
        {
            logger.LogDebug("Cache hit for all roles");
            return cachedRoles;
        }

        logger.LogDebug("Cache miss for all roles, fetching from database");
        var roles = await repository.GetAllAsync(cancellationToken);
        
        await cacheService.SetAsync(AllRolesKey, roles, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached all roles ({Count} roles)", roles.Count);

        return roles;
    }

    public async Task<IReadOnlyList<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        var cachedRoles = await cacheService.GetAsync<IReadOnlyList<Role>>(ActiveRolesKey, cancellationToken);
        if (cachedRoles is not null)
        {
            logger.LogDebug("Cache hit for active roles");
            return cachedRoles;
        }

        logger.LogDebug("Cache miss for active roles, fetching from database");
        var roles = await repository.GetActiveRolesAsync(cancellationToken);
        
        await cacheService.SetAsync(ActiveRolesKey, roles, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached active roles ({Count} roles)", roles.Count);

        return roles;
    }

    public async Task<IReadOnlyList<Role>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(PagedRolesKey, pageNumber, pageSize);
        
        var cachedRoles = await cacheService.GetAsync<IReadOnlyList<Role>>(cacheKey, cancellationToken);
        if (cachedRoles is not null)
        {
            logger.LogDebug("Cache hit for paged roles (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return cachedRoles;
        }

        logger.LogDebug("Cache miss for paged roles (page {PageNumber}, size {PageSize}), fetching from database", pageNumber, pageSize);
        var roles = await repository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, roles, ShortExpiration, cancellationToken);
        logger.LogDebug("Cached paged roles (page {PageNumber}, size {PageSize}, {Count} roles)", pageNumber, pageSize, roles.Count);

        return roles;
    }

    public async Task<IReadOnlyList<Role>> GetRolesWithPermissionAsync(PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RolesWithPermissionKey, permission.Resource, permission.Action, permission.Scope);
        
        var cachedRoles = await cacheService.GetAsync<IReadOnlyList<Role>>(cacheKey, cancellationToken);
        if (cachedRoles is not null)
        {
            logger.LogDebug("Cache hit for roles with permission {Resource}-{Action}-{Scope}", 
                permission.Resource, permission.Action, permission.Scope);
            return cachedRoles;
        }

        logger.LogDebug("Cache miss for roles with permission {Resource}-{Action}-{Scope}, fetching from database", 
            permission.Resource, permission.Action, permission.Scope);
        var roles = await repository.GetRolesWithPermissionAsync(permission, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, roles, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached roles with permission {Resource}-{Action}-{Scope} ({Count} roles)", 
            permission.Resource, permission.Action, permission.Scope, roles.Count);

        return roles;
    }

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<RoleId> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Select(id => id.Value).ToList();
        var cacheKey = string.Format(RolesByIdsKey, string.Join(",", ids.OrderBy(x => x)));
        
        var cachedRoles = await cacheService.GetAsync<IReadOnlyList<Role>>(cacheKey, cancellationToken);
        if (cachedRoles is not null)
        {
            logger.LogDebug("Cache hit for roles by IDs: {RoleIds}", string.Join(", ", ids));
            return cachedRoles;
        }

        logger.LogDebug("Cache miss for roles by IDs: {RoleIds}, fetching from database", string.Join(", ", ids));
        var roles = await repository.GetByIdsAsync(roleIds, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, roles, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached roles by IDs: {RoleIds} ({Count} roles)", string.Join(", ", ids), roles.Count);

        return roles;
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(role, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateRoleCaches(role.Id, role.Name.Value, cancellationToken);
        logger.LogDebug("Added role and invalidated caches for role ID {RoleId}", role.Id);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await repository.UpdateAsync(role, cancellationToken);
        
        // Invalidate relevant caches
        await InvalidateRoleCaches(role.Id, role.Name.Value, cancellationToken);
        logger.LogDebug("Updated role and invalidated caches for role ID {RoleId}", role.Id);
    }

    public async Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        // Get role first to get name for cache invalidation
        var role = await repository.GetByIdAsync(id, cancellationToken);
        
        await repository.DeleteAsync(id, cancellationToken);
        
        if (role is not null)
        {
            await InvalidateRoleCaches(role.Id, role.Name.Value, cancellationToken);
            logger.LogDebug("Deleted role and invalidated caches for role ID {RoleId}", id.Value);
        }
    }

    public async Task SoftDeleteAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        // Get role first to get name for cache invalidation
        var role = await repository.GetByIdAsync(id, cancellationToken);
        
        await repository.SoftDeleteAsync(id, cancellationToken);
        
        if (role is not null)
        {
            await InvalidateRoleCaches(role.Id, role.Name.Value, cancellationToken);
            logger.LogDebug("Soft deleted role and invalidated caches for role ID {RoleId}", id.Value);
        }
    }

    public async Task AddPermissionToRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        await repository.AddPermissionToRoleAsync(roleId, permission, cancellationToken);
        
        // Invalidate role-specific caches
        await InvalidateRolePermissionCaches(roleId, cancellationToken);
        logger.LogDebug("Added permission to role and invalidated caches for role ID {RoleId}", roleId.Value);
    }

    public async Task RemovePermissionFromRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default)
    {
        await repository.RemovePermissionFromRoleAsync(roleId, permission, cancellationToken);
        
        // Invalidate role-specific caches
        await InvalidateRolePermissionCaches(roleId, cancellationToken);
        logger.LogDebug("Removed permission from role and invalidated caches for role ID {RoleId}", roleId.Value);
    }

    public async Task SetRolePermissionsAsync(RoleId roleId, List<PermissionEntity> permissions, CancellationToken cancellationToken = default)
    {
        await repository.SetRolePermissionsAsync(roleId, permissions, cancellationToken);
        
        // Invalidate role-specific caches
        await InvalidateRolePermissionCaches(roleId, cancellationToken);
        logger.LogDebug("Set permissions for role and invalidated caches for role ID {RoleId}", roleId.Value);
    }

    public async Task<IReadOnlyList<PermissionEntity>> GetRolePermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RolePermissionsKey, roleId.Value);
        
        var cachedPermissions = await cacheService.GetAsync<IReadOnlyList<PermissionEntity>>(cacheKey, cancellationToken);
        if (cachedPermissions is not null)
        {
            logger.LogDebug("Cache hit for role permissions {RoleId}", roleId.Value);
            return cachedPermissions;
        }

        logger.LogDebug("Cache miss for role permissions {RoleId}, fetching from database", roleId.Value);
        var permissions = await repository.GetRolePermissionsAsync(roleId, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, permissions, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached role permissions for {RoleId} ({Count} permissions)", roleId.Value, permissions.Count);

        return permissions;
    }

    public async Task<bool> ExistsAsync(RoleId id, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RoleExistsKey, id.Value);
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for role existence check ID {RoleId}", id.Value);
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for role existence check ID {RoleId}, checking database", id.Value);
        var exists = await repository.ExistsAsync(id, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached role existence check for ID {RoleId}: {Exists}", id.Value, exists);

        return exists;
    }

    public async Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RoleExistsByNameKey, name.Value);
        
        var cachedExists = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedExists.HasValue)
        {
            logger.LogDebug("Cache hit for role existence check name {RoleName}", name.Value);
            return cachedExists.Value;
        }

        logger.LogDebug("Cache miss for role existence check name {RoleName}, checking database", name.Value);
        var exists = await repository.ExistsByNameAsync(name, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, exists, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached role existence check for name {RoleName}: {Exists}", name.Value, exists);

        return exists;
    }

    public async Task<bool> RoleHasPermissionAsync(RoleId roleId, PermissionValueObject permission, CancellationToken cancellationToken = default)
    {
        var cacheKey = string.Format(RoleHasPermissionKey, roleId.Value, permission.Resource, permission.Action, permission.Scope);
        
        var cachedHasPermission = await cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cachedHasPermission.HasValue)
        {
            logger.LogDebug("Cache hit for role permission check {RoleId} - {Resource}-{Action}-{Scope}", 
                roleId.Value, permission.Resource, permission.Action, permission.Scope);
            return cachedHasPermission.Value;
        }

        logger.LogDebug("Cache miss for role permission check {RoleId} - {Resource}-{Action}-{Scope}, checking database", 
            roleId.Value, permission.Resource, permission.Action, permission.Scope);
        var hasPermission = await repository.RoleHasPermissionAsync(roleId, permission, cancellationToken);
        
        await cacheService.SetAsync(cacheKey, hasPermission, DefaultExpiration, cancellationToken);
        logger.LogDebug("Cached role permission check for {RoleId} - {Resource}-{Action}-{Scope}: {HasPermission}", 
            roleId.Value, permission.Resource, permission.Action, permission.Scope, hasPermission);

        return hasPermission;
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(RoleCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for role count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for role count, fetching from database");
        var count = await repository.GetCountAsync(cancellationToken);
        
        await cacheService.SetAsync(RoleCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached role count: {Count}", count);

        return count;
    }

    public async Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var cachedCount = await cacheService.GetAsync<int?>(ActiveRoleCountKey, cancellationToken);
        if (cachedCount.HasValue)
        {
            logger.LogDebug("Cache hit for active role count");
            return cachedCount.Value;
        }

        logger.LogDebug("Cache miss for active role count, fetching from database");
        var count = await repository.GetActiveCountAsync(cancellationToken);
        
        await cacheService.SetAsync(ActiveRoleCountKey, count, CountExpiration, cancellationToken);
        logger.LogDebug("Cached active role count: {Count}", count);

        return count;
    }

    private async Task InvalidateRoleCaches(Guid roleId, string roleName, CancellationToken cancellationToken)
    {
        // Invalidate specific role caches
        await cacheService.RemoveAsync(string.Format(RoleByIdKey, roleId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(RoleByNameKey, roleName), cancellationToken);
        await cacheService.RemoveAsync(string.Format(RoleExistsKey, roleId), cancellationToken);
        await cacheService.RemoveAsync(string.Format(RoleExistsByNameKey, roleName), cancellationToken);
        
        // Invalidate list caches
        await cacheService.RemoveAsync(AllRolesKey, cancellationToken);
        await cacheService.RemoveAsync(ActiveRolesKey, cancellationToken);
        await cacheService.RemoveAsync(RoleCountKey, cancellationToken);
        await cacheService.RemoveAsync(ActiveRoleCountKey, cancellationToken);
        
        // Invalidate paged and complex query caches using patterns
        await cacheService.RemoveByPatternAsync("roles:paged:*", cancellationToken);
        await cacheService.RemoveByPatternAsync("roles:ids:*", cancellationToken);
        await cacheService.RemoveByPatternAsync("roles:permission:*", cancellationToken);
        
        // Invalidate role-specific permission caches
        await InvalidateRolePermissionCaches(new RoleId(roleId), cancellationToken);
        
        // Invalidate using tags if supported
        await cacheService.RemoveByTagAsync(RolesTag, cancellationToken);
        await cacheService.RemoveByTagAsync(string.Format(RoleTag, roleId), cancellationToken);
    }

    private async Task InvalidateRolePermissionCaches(RoleId roleId, CancellationToken cancellationToken)
    {
        // Invalidate role permissions cache
        await cacheService.RemoveAsync(string.Format(RolePermissionsKey, roleId.Value), cancellationToken);
        
        // Invalidate role permission check caches using pattern
        await cacheService.RemoveByPatternAsync($"role:haspermission:{roleId.Value}:*", cancellationToken);
        
        // Invalidate the role itself to refresh permissions
        await cacheService.RemoveAsync(string.Format(RoleByIdKey, roleId.Value), cancellationToken);
        
        // Invalidate complex queries that depend on permissions
        await cacheService.RemoveByPatternAsync("roles:permission:*", cancellationToken);
    }
}