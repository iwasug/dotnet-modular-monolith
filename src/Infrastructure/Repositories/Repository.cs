using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Extensions;
using ModularMonolith.Shared.Domain;
using ModularMonolith.Shared.Interfaces;
using System.Linq.Expressions;

namespace ModularMonolith.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with common CRUD operations, transaction support, and query optimizations
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
internal class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly ILogger<Repository<TEntity>> _logger;

    public Repository(ApplicationDbContext context, ILogger<Repository<TEntity>> logger)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _logger = logger;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        return await _dbSet
            .AsNoTracking()
            .TagWith($"Repository.GetById: {typeof(TEntity).Name}")
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all {EntityType} entities", typeof(TEntity).Name);
        
        return await _dbSet
            .AsNoTracking()
            .OrderByOptimized(e => e.CreatedAt)
            .TagWith($"Repository.GetAll: {typeof(TEntity).Name}")
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedWithCountAsync(
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged {EntityType} entities with count - Page: {PageNumber}, Size: {PageSize}", 
            typeof(TEntity).Name, pageNumber, pageSize);
        
        return await _dbSet
            .AsNoTracking()
            .OrderByOptimized(e => e.CreatedAt)
            .TagWith($"Repository.GetPagedWithCount: {typeof(TEntity).Name}")
            .GetPagedWithCountAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged {EntityType} entities - Page: {PageNumber}, Size: {PageSize}", 
            typeof(TEntity).Name, pageNumber, pageSize);
        
        var (items, _) = await GetPagedWithCountAsync(pageNumber, pageSize, cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding {EntityType} entities with predicate", typeof(TEntity).Name);
        
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .OrderByOptimized(e => e.CreatedAt)
            .TagWith($"Repository.Find: {typeof(TEntity).Name}")
            .ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> FindFirstAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding first {EntityType} entity with predicate", typeof(TEntity).Name);
        
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .TagWith($"Repository.FindFirst: {typeof(TEntity).Name}")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Adding {EntityType} with ID {Id}", typeof(TEntity).Name, entity.Id);
        
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities is null)
            throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        _logger.LogDebug("Adding {Count} {EntityType} entities", entityList.Count, typeof(TEntity).Name);
        
        await _dbSet.AddRangeAsync(entityList, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Updating {EntityType} with ID {Id}", typeof(TEntity).Name, entity.Id);
        
        // Audit information will be set automatically by DbContext
        _dbSet.Update(entity);
        
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities is null)
            throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        _logger.LogDebug("Updating {Count} {EntityType} entities", entityList.Count, typeof(TEntity).Name);
        
        // Audit information will be set automatically by DbContext
        _dbSet.UpdateRange(entityList);
        return Task.CompletedTask;
    }



    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Soft deleting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is not null)
        {
            entity.SoftDelete();
            _dbSet.Update(entity);
        }
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Soft deleting {EntityType} with ID {Id}", typeof(TEntity).Name, entity.Id);
        
        entity.SoftDelete();
        _dbSet.Update(entity);
        
        return Task.CompletedTask;
    }

    public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Hard deleting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        var entity = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is not null)
        {
            _dbSet.Remove(entity);
        }
    }

    public Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _logger.LogDebug("Hard deleting {EntityType} with ID {Id}", typeof(TEntity).Name, entity.Id);
        
        _dbSet.Remove(entity);
        
        return Task.CompletedTask;
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Restoring {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        var entity = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is not null && entity.IsDeleted)
        {
            entity.Restore();
            _dbSet.Update(entity);
        }
    }

    public async Task<int> BulkDeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Bulk deleting {EntityType} entities", typeof(TEntity).Name);
        
        return await _dbSet
            .Where(predicate)
            .BulkDeleteAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of {EntityType} with ID {Id}", typeof(TEntity).Name, id);
        
        return await _dbSet.ExistsOptimizedAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking existence of {EntityType} with predicate", typeof(TEntity).Name);
        
        return await _dbSet.ExistsOptimizedAsync(predicate, cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting count of {EntityType} entities", typeof(TEntity).Name);
        
        return await _dbSet.CountAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting count of {EntityType} entities with predicate", typeof(TEntity).Name);
        
        return await _dbSet
            .Where(predicate)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetDeletedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting deleted {EntityType} entities", typeof(TEntity).Name);
        
        return await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.IsDeleted)
            .OrderByOptimized(e => e.DeletedAt)
            .TagWith($"Repository.GetDeleted: {typeof(TEntity).Name}")
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all {EntityType} entities including deleted", typeof(TEntity).Name);
        
        return await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByOptimized(e => e.CreatedAt)
            .TagWith($"Repository.GetAllIncludingDeleted: {typeof(TEntity).Name}")
            .ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting {EntityType} with ID {Id} including deleted", typeof(TEntity).Name, id);
        
        return await _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .TagWith($"Repository.GetByIdIncludingDeleted: {typeof(TEntity).Name}")
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to database");
        
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    /// <summary>
    /// Gets entities with optimized includes to avoid N+1 queries
    /// </summary>
    protected virtual IQueryable<TEntity> GetQueryWithIncludes(params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking();
        
        foreach (var include in includes)
        {
            query = query.IncludeOptimized(include, splitQuery: includes.Length > 2);
        }
        
        return query;
    }

    /// <summary>
    /// Applies common query optimizations
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyOptimizations(IQueryable<TEntity> query)
    {
        return query
            .AsNoTracking()
            .TagWith($"Repository: {typeof(TEntity).Name}")
            .UseIndex($"IX_{typeof(TEntity).Name}_CreatedAt");
    }
}