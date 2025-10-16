using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace ModularMonolith.Infrastructure.Performance;

/// <summary>
/// Interceptor for monitoring EF Core query performance and identifying slow queries
/// </summary>
public sealed class QueryPerformanceMonitor(
    ILogger<QueryPerformanceMonitor> logger,
    TimeSpan? slowQueryThreshold = null)
    : DbCommandInterceptor
{
    private readonly ConcurrentDictionary<Guid, QueryMetrics> _activeQueries = new();
    private readonly TimeSpan _slowQueryThreshold = slowQueryThreshold ?? TimeSpan.FromMilliseconds(1000); // 1 second default

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        var queryId = Guid.NewGuid();
        var metrics = new QueryMetrics
        {
            QueryId = queryId,
            CommandText = command.CommandText,
            StartTime = DateTime.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        _activeQueries[queryId] = metrics;
        
        // Add query ID to command for tracking
        command.CommandText = $"/* QueryId: {queryId} */ {command.CommandText}";

        logger.LogDebug("Query started: {QueryId} - {CommandText}", 
            queryId, TruncateQuery(command.CommandText));

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var queryId = ExtractQueryId(command.CommandText);
        
        if (queryId.HasValue && _activeQueries.TryRemove(queryId.Value, out var metrics))
        {
            metrics.Stopwatch.Stop();
            metrics.EndTime = DateTime.UtcNow;
            metrics.Duration = metrics.Stopwatch.Elapsed;
            metrics.IsSuccessful = true;

            LogQueryCompletion(metrics);
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }



    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var queryId = Guid.NewGuid();
        var metrics = new QueryMetrics
        {
            QueryId = queryId,
            CommandText = command.CommandText,
            StartTime = DateTime.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        _activeQueries[queryId] = metrics;
        command.CommandText = $"/* QueryId: {queryId} */ {command.CommandText}";

        logger.LogDebug("Non-query started: {QueryId} - {CommandText}", 
            queryId, TruncateQuery(command.CommandText));

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var queryId = ExtractQueryId(command.CommandText);
        
        if (queryId.HasValue && _activeQueries.TryRemove(queryId.Value, out var metrics))
        {
            metrics.Stopwatch.Stop();
            metrics.EndTime = DateTime.UtcNow;
            metrics.Duration = metrics.Stopwatch.Elapsed;
            metrics.IsSuccessful = true;
            metrics.RowsAffected = result;

            LogQueryCompletion(metrics);
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }



    private void LogQueryCompletion(QueryMetrics metrics)
    {
        var isSlowQuery = metrics.Duration > _slowQueryThreshold;
        
        if (isSlowQuery)
        {
            logger.LogWarning("Slow query detected: {QueryId} took {Duration}ms - {CommandText}",
                metrics.QueryId,
                metrics.Duration.TotalMilliseconds,
                TruncateQuery(metrics.CommandText));
        }
        else
        {
            logger.LogDebug("Query completed: {QueryId} took {Duration}ms - Rows affected: {RowsAffected}",
                metrics.QueryId,
                metrics.Duration.TotalMilliseconds,
                metrics.RowsAffected);
        }

        // Log structured metrics for monitoring systems
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["QueryId"] = metrics.QueryId,
            ["Duration"] = metrics.Duration.TotalMilliseconds,
            ["IsSlowQuery"] = isSlowQuery,
            ["RowsAffected"] = metrics.RowsAffected ?? 0,
            ["QueryType"] = GetQueryType(metrics.CommandText)
        });

        if (isSlowQuery)
        {
            logger.LogInformation("Query performance metrics logged for slow query");
        }
    }

    private void LogQueryFailure(QueryMetrics metrics)
    {
        logger.LogError(metrics.Exception, 
            "Query failed: {QueryId} after {Duration}ms - {CommandText}",
            metrics.QueryId,
            metrics.Duration.TotalMilliseconds,
            TruncateQuery(metrics.CommandText));

        // Log structured metrics for monitoring systems
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["QueryId"] = metrics.QueryId,
            ["Duration"] = metrics.Duration.TotalMilliseconds,
            ["IsFailure"] = true,
            ["QueryType"] = GetQueryType(metrics.CommandText)
        });

        logger.LogInformation("Query failure metrics logged");
    }

    private static Guid? ExtractQueryId(string commandText)
    {
        const string prefix = "/* QueryId: ";
        const string suffix = " */";
        
        var startIndex = commandText.IndexOf(prefix, StringComparison.Ordinal);
        if (startIndex == -1) return null;
        
        startIndex += prefix.Length;
        var endIndex = commandText.IndexOf(suffix, startIndex, StringComparison.Ordinal);
        if (endIndex == -1) return null;
        
        var guidString = commandText.Substring(startIndex, endIndex - startIndex);
        return Guid.TryParse(guidString, out var guid) ? guid : null;
    }

    private static string TruncateQuery(string query, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(query) || query.Length <= maxLength)
            return query;
            
        return query.Substring(0, maxLength) + "...";
    }

    private static string GetQueryType(string commandText)
    {
        if (string.IsNullOrEmpty(commandText))
            return "Unknown";
            
        var trimmed = commandText.TrimStart();
        
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return "SELECT";
        if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            return "INSERT";
        if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            return "UPDATE";
        if (trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            return "DELETE";
        if (trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
            return "CREATE";
        if (trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase))
            return "ALTER";
        if (trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase))
            return "DROP";
            
        return "Other";
    }

    private sealed class QueryMetrics
    {
        public Guid QueryId { get; set; }
        public string CommandText { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Stopwatch Stopwatch { get; set; } = new();
        public bool IsSuccessful { get; set; }
        public int? RowsAffected { get; set; }
        public Exception? Exception { get; set; }
    }
}