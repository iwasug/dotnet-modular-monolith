using ModularMonolith.Roles.Domain.ValueObjects;
using PermissionEntity = ModularMonolith.Shared.Domain.Permission;
using PermissionValueObject = ModularMonolith.Roles.Domain.ValueObjects.Permission;

namespace ModularMonolith.Roles.Domain;

/// <summary>
/// Repository interface for role operations with async methods and CancellationToken support
/// Provides comprehensive CRUD operations and permission management following repository pattern
/// </summary>
public interface IRoleRepository
{
    // Query operations
    /// <summary>
    /// Gets a role by its unique identifier
    /// </summary>
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a role by its name
    /// </summary>
    Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all roles in the system
    /// </summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active roles only
    /// </summary>
    Task<IReadOnlyList<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets roles with pagination support
    /// </summary>
    Task<IReadOnlyList<Role>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets roles that contain a specific permission
    /// </summary>
    Task<IReadOnlyList<Role>> GetRolesWithPermissionAsync(PermissionValueObject permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets roles by a list of role IDs
    /// </summary>
    Task<IReadOnlyList<Role>> GetByIdsAsync(IEnumerable<RoleId> roleIds, CancellationToken cancellationToken = default);

    // Command operations
    /// <summary>
    /// Adds a new role to the repository
    /// </summary>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing role in the repository
    /// </summary>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a role from the repository (hard delete)
    /// </summary>
    Task DeleteAsync(RoleId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes a role by deactivating it
    /// </summary>
    Task SoftDeleteAsync(RoleId id, CancellationToken cancellationToken = default);

    // Permission management operations
    /// <summary>
    /// Adds a permission to a role
    /// </summary>
    Task AddPermissionToRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a permission from a role
    /// </summary>
    Task RemovePermissionFromRoleAsync(RoleId roleId, PermissionEntity permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets all permissions for a role (replaces existing permissions)
    /// </summary>
    Task SetRolePermissionsAsync(RoleId roleId, List<PermissionEntity> permissions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    Task<IReadOnlyList<PermissionEntity>> GetRolePermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default);

    // Existence checks
    /// <summary>
    /// Checks if a role exists by its unique identifier
    /// </summary>
    Task<bool> ExistsAsync(RoleId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a role exists by its name
    /// </summary>
    Task<bool> ExistsByNameAsync(RoleName name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    Task<bool> RoleHasPermissionAsync(RoleId roleId, PermissionValueObject permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of roles in the system
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of active roles in the system
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}