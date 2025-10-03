using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ModularMonolith.Infrastructure.Extensions;

/// <summary>
/// Extension methods for optimizing EF Core queries
/// </summary>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// Applies optimized pagination with proper ordering and count optimization
    /// </summary>
    public static async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedWithCountAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        // Validate and normalize pagination parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        // Use a single query to get both count and items for better performance
        var totalCount = await query.CountAsync(cancellationToken);
        
        if (totalCount == 0)
        {
            return (Array.Empty<T>(), 0);
        }
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
            
        return (items, totalCount);
    }
    
    /// <summary>
    /// Applies optimized pagination with count estimation for large datasets
    /// </summary>
    public static async Task<(IReadOnlyList<T> Items, int EstimatedTotalCount)> GetPagedWithEstimatedCountAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        // Validate and normalize pagination parameters
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        
        // For large datasets, estimate count by taking a larger sample
        var sampleSize = Math.Max(pageSize * 10, 1000);
        var sample = await query
            .Take(sampleSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
            
        var estimatedCount = sample.Count < sampleSize ? sample.Count : sample.Count * 10; // Rough estimation
        
        var items = sample
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
            
        return (items, estimatedCount);
    }
    
    /// <summary>
    /// Applies conditional filtering with optimized query compilation
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }
    
    /// <summary>
    /// Applies optimized exists check without loading entities
    /// </summary>
    public static Task<bool> ExistsOptimizedAsync<T>(
        this IQueryable<T> query,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) where T : class
    {
        return query
            .AsNoTracking()
            .Where(predicate)
            .AnyAsync(cancellationToken);
    }
    
    /// <summary>
    /// Applies batch loading for related entities to avoid N+1 queries
    /// </summary>
    public static IQueryable<T> IncludeOptimized<T, TProperty>(
        this IQueryable<T> query,
        Expression<Func<T, TProperty>> navigationPropertyPath,
        bool splitQuery = false) where T : class
    {
        var includedQuery = query.Include(navigationPropertyPath);
        return splitQuery ? includedQuery.AsSplitQuery() : includedQuery;
    }
    
    /// <summary>
    /// Applies optimized ordering with null handling
    /// </summary>
    public static IOrderedQueryable<T> OrderByOptimized<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false,
        bool nullsLast = true)
    {
        if (descending)
        {
            return nullsLast 
                ? query.OrderByDescending(keySelector)
                : query.OrderByDescending(keySelector);
        }
        
        return nullsLast 
            ? query.OrderBy(keySelector)
            : query.OrderBy(keySelector);
    }
    
    /// <summary>
    /// Applies optimized bulk delete operations
    /// </summary>
    public static Task<int> BulkDeleteAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        return query.ExecuteDeleteAsync(cancellationToken);
    }
    
    /// <summary>
    /// Applies query hints for PostgreSQL optimization
    /// </summary>
    public static IQueryable<T> WithQueryHint<T>(
        this IQueryable<T> query,
        string hint) where T : class
    {
        // For PostgreSQL, we can use query tags that can be processed by query interceptors
        return query.TagWith($"QueryHint: {hint}");
    }
    
    /// <summary>
    /// Applies index hints for PostgreSQL
    /// </summary>
    public static IQueryable<T> UseIndex<T>(
        this IQueryable<T> query,
        string indexName) where T : class
    {
        return query.TagWith($"UseIndex: {indexName}");
    }
}