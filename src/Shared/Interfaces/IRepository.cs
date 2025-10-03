using ModularMonolith.Shared.Domain;
using System.Linq.Expressions;

namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Generic repository interface for common CRUD operations with soft delete support
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    // Query operations
    /// <summary>
    /// Gets an entity by its unique identifier (excludes soft deleted)
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an entity by its unique identifier including soft deleted entities
    /// </summary>
    Task<TEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities (excludes soft deleted)
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities including soft deleted entities
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets only soft deleted entities
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetDeletedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets entities with pagination support (excludes soft deleted)
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets entities with pagination and total count (excludes soft deleted)
    /// </summary>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedWithCountAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds entities matching the predicate (excludes soft deleted)
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the first entity matching the predicate (excludes soft deleted)
    /// </summary>
    Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    // Command operations
    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds multiple entities to the repository
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates multiple entities in the repository
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    

    
    /// <summary>
    /// Soft deletes an entity by its unique identifier
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes an entity
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently removes an entity from the repository (hard delete)
    /// </summary>
    Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Permanently removes an entity from the repository (hard delete)
    /// </summary>
    Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs bulk hard delete on entities matching the predicate
    /// </summary>
    Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores a soft deleted entity
    /// </summary>
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    // Existence checks
    /// <summary>
    /// Checks if an entity exists by its unique identifier (excludes soft deleted)
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if an entity exists matching the predicate (excludes soft deleted)
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of entities (excludes soft deleted)
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of entities matching the predicate (excludes soft deleted)
    /// </summary>
    Task<int> GetCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    // Transaction support
    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}