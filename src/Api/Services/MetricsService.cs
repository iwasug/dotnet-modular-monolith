using System.Collections.Concurrent;
using System.Diagnostics;

namespace ModularMonolith.Api.Services;

/// <summary>
/// In-memory implementation of metrics service for collecting application performance metrics
/// </summary>
public sealed class MetricsService : IMetricsService
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();
    private readonly ConcurrentDictionary<string, List<double>> _histograms = new();
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics = new();
    private readonly object _lock = new();

    public void RecordRequestDuration(string endpoint, string method, int statusCode, double durationMs)
    {
        var key = $"{method}_{endpoint}";
        
        _requestMetrics.AddOrUpdate(key, 
            new RequestMetrics
            {
                Endpoint = endpoint,
                Method = method,
                TotalRequests = 1,
                TotalDuration = durationMs,
                MinDuration = durationMs,
                MaxDuration = durationMs,
                StatusCodes = new ConcurrentDictionary<int, long> { [statusCode] = 1 }
            },
            (_, existing) =>
            {
                existing.TotalRequests++;
                existing.TotalDuration += durationMs;
                existing.MinDuration = Math.Min(existing.MinDuration, durationMs);
                existing.MaxDuration = Math.Max(existing.MaxDuration, durationMs);
                existing.StatusCodes.AddOrUpdate(statusCode, 1, (_, count) => count + 1);
                return existing;
            });
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        _counters.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        _gauges.AddOrUpdate(key, value, (_, _) => value);
    }

    public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        
        lock (_lock)
        {
            if (!_histograms.ContainsKey(key))
            {
                _histograms[key] = new List<double>();
            }
            
            _histograms[key].Add(value);
            
            // Keep only the last 1000 values to prevent memory issues
            if (_histograms[key].Count > 1000)
            {
                _histograms[key].RemoveAt(0);
            }
        }
    }

    public Task<Dictionary<string, object>> GetMetricsAsync()
    {
        var metrics = new Dictionary<string, object>();

        // Add counters
        if (_counters.Any())
        {
            metrics["counters"] = _counters.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        // Add gauges
        if (_gauges.Any())
        {
            metrics["gauges"] = _gauges.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        // Add histogram statistics
        if (_histograms.Any())
        {
            var histogramStats = new Dictionary<string, object>();
            
            lock (_lock)
            {
                foreach (var kvp in _histograms)
                {
                    var values = kvp.Value.ToArray();
                    if (values.Length > 0)
                    {
                        Array.Sort(values);
                        histogramStats[kvp.Key] = new
                        {
                            Count = values.Length,
                            Min = values[0],
                            Max = values[^1],
                            Mean = values.Average(),
                            P50 = GetPercentile(values, 0.5),
                            P95 = GetPercentile(values, 0.95),
                            P99 = GetPercentile(values, 0.99)
                        };
                    }
                }
            }
            
            if (histogramStats.Any())
            {
                metrics["histograms"] = histogramStats;
            }
        }

        // Add request metrics
        if (_requestMetrics.Any())
        {
            var requestStats = _requestMetrics.Values.Select(rm => new
            {
                rm.Endpoint,
                rm.Method,
                rm.TotalRequests,
                AverageDuration = rm.TotalDuration / rm.TotalRequests,
                rm.MinDuration,
                rm.MaxDuration,
                StatusCodes = rm.StatusCodes.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
            }).ToArray();
            
            metrics["requests"] = requestStats;
        }

        // Add system metrics
        var process = Process.GetCurrentProcess();
        metrics["system"] = new
        {
            WorkingSet = process.WorkingSet64,
            PrivateMemorySize = process.PrivateMemorySize64,
            VirtualMemorySize = process.VirtualMemorySize64,
            ProcessorTime = process.TotalProcessorTime.TotalMilliseconds,
            ThreadCount = process.Threads.Count,
            HandleCount = process.HandleCount,
            StartTime = process.StartTime,
            Uptime = DateTime.UtcNow - process.StartTime
        };

        return Task.FromResult(metrics);
    }

    private static string BuildKey(string name, Dictionary<string, string>? tags)
    {
        if (tags is null || !tags.Any())
        {
            return name;
        }

        var tagString = string.Join(",", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}[{tagString}]";
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0) return 0;
        if (sortedValues.Length == 1) return sortedValues[0];

        var index = percentile * (sortedValues.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
        {
            return sortedValues[lower];
        }

        var weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }

    private sealed class RequestMetrics
    {
        public string Endpoint { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public long TotalRequests { get; set; }
        public double TotalDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public ConcurrentDictionary<int, long> StatusCodes { get; init; } = new();
    }
}