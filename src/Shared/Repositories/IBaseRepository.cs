using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Shared.Repositories;

/// <summary>
/// Base repository interface for common CRUD operations
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    // Query operations
    /// <summary>
    /// Gets an entity by its unique identifier
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets entities with pagination support
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Command operations
    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an entity from the repository (hard delete)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an entity from the repository (hard delete)
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    // Existence checks
    /// <summary>
    /// Checks if an entity exists by its unique identifier
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of entities
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    // Transaction support
    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}