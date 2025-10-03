namespace ModularMonolith.Api.Services;

/// <summary>
/// Service for collecting and exposing application performance metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Records a request duration metric
    /// </summary>
    void RecordRequestDuration(string endpoint, string method, int statusCode, double durationMs);

    /// <summary>
    /// Increments a counter metric
    /// </summary>
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Records a gauge metric
    /// </summary>
    void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Records a histogram metric
    /// </summary>
    void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Gets current metrics snapshot
    /// </summary>
    Task<Dictionary<string, object>> GetMetricsAsync();
}