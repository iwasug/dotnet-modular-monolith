using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain;

/// <summary>
/// Repository interface for user operations with async methods and CancellationToken support
/// Provides comprehensive CRUD operations following repository pattern
/// </summary>
public interface IUserRepository
{
    // Query operations
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all users in the system
    /// </summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active users only
    /// </summary>
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets users with pagination support
    /// </summary>
    Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Command operations
    /// <summary>
    /// Adds a new user to the repository
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user in the repository
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a user from the repository (hard delete)
    /// </summary>
    Task DeleteAsync(UserId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Soft deletes a user by deactivating them
    /// </summary>
    Task SoftDeleteAsync(UserId id, CancellationToken cancellationToken = default);

    // Existence checks
    /// <summary>
    /// Checks if a user exists by their unique identifier
    /// </summary>
    Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user exists by their email address
    /// </summary>
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total count of users in the system
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of active users in the system
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken cancellationToken = default);
}