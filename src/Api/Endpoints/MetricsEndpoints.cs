using ModularMonolith.Api.Services;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Endpoints for exposing application metrics
/// </summary>
public static class MetricsEndpoints
{
    /// <summary>
    /// Maps metrics endpoints
    /// </summary>
    public static void MapMetricsEndpoints(this WebApplication app)
    {
        var metrics = app.MapGroup("/metrics")
            .WithTags("Metrics")
            .WithOpenApi();

        // Public metrics endpoint (basic metrics)
        metrics.MapGet("/", GetBasicMetrics)
            .WithName("GetBasicMetrics")
            .WithSummary("Get basic application metrics")
            .WithDescription("Returns basic application performance metrics")
            .Produces<object>(StatusCodes.Status200OK);

        // Detailed metrics endpoint (requires authorization)
        metrics.MapGet("/detailed", GetDetailedMetrics)
            .WithName("GetDetailedMetrics")
            .WithSummary("Get detailed application metrics")
            .WithDescription("Returns comprehensive application performance metrics and system information")
            .RequireAuthorization()
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // Prometheus-style metrics endpoint
        metrics.MapGet("/prometheus", GetPrometheusMetrics)
            .WithName("GetPrometheusMetrics")
            .WithSummary("Get metrics in Prometheus format")
            .WithDescription("Returns metrics in Prometheus exposition format")
            .Produces<string>(StatusCodes.Status200OK, "text/plain");
    }

    private static async Task<IResult> GetBasicMetrics(IMetricsService metricsService)
    {
        try
        {
            var allMetrics = await metricsService.GetMetricsAsync();
            
            // Return only basic metrics for public consumption
            var basicMetrics = new
            {
                timestamp = DateTime.UtcNow,
                uptime = allMetrics.ContainsKey("system") ? 
                    ((dynamic)allMetrics["system"]).Uptime : 
                    TimeSpan.Zero,
                requests = allMetrics.ContainsKey("requests") ? 
                    ((object[])allMetrics["requests"]).Length : 
                    0,
                status = "healthy"
            };

            return Results.Ok(basicMetrics);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to retrieve metrics",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetDetailedMetrics(IMetricsService metricsService)
    {
        try
        {
            var metrics = await metricsService.GetMetricsAsync();
            
            var response = new
            {
                timestamp = DateTime.UtcNow,
                metrics = metrics
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to retrieve detailed metrics",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetPrometheusMetrics(IMetricsService metricsService)
    {
        try
        {
            var metrics = await metricsService.GetMetricsAsync();
            var prometheusFormat = ConvertToPrometheusFormat(metrics);
            
            return Results.Text(prometheusFormat, "text/plain; version=0.0.4");
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Failed to retrieve Prometheus metrics",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static string ConvertToPrometheusFormat(Dictionary<string, object> metrics)
    {
        var lines = new List<string>();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Add counters
        if (metrics.ContainsKey("counters"))
        {
            var counters = (Dictionary<string, object>)metrics["counters"];
            foreach (var counter in counters)
            {
                var metricName = SanitizeMetricName(counter.Key);
                lines.Add($"# TYPE {metricName} counter");
                lines.Add($"{metricName} {counter.Value} {timestamp}");
            }
        }

        // Add gauges
        if (metrics.ContainsKey("gauges"))
        {
            var gauges = (Dictionary<string, object>)metrics["gauges"];
            foreach (var gauge in gauges)
            {
                var metricName = SanitizeMetricName(gauge.Key);
                lines.Add($"# TYPE {metricName} gauge");
                lines.Add($"{metricName} {gauge.Value} {timestamp}");
            }
        }

        // Add system metrics
        if (metrics.ContainsKey("system"))
        {
            var system = (dynamic)metrics["system"];
            
            lines.Add("# TYPE process_working_set_bytes gauge");
            lines.Add($"process_working_set_bytes {system.WorkingSet} {timestamp}");
            
            lines.Add("# TYPE process_private_memory_bytes gauge");
            lines.Add($"process_private_memory_bytes {system.PrivateMemorySize} {timestamp}");
            
            lines.Add("# TYPE process_virtual_memory_bytes gauge");
            lines.Add($"process_virtual_memory_bytes {system.VirtualMemorySize} {timestamp}");
            
            lines.Add("# TYPE process_cpu_seconds_total counter");
            lines.Add($"process_cpu_seconds_total {system.ProcessorTime / 1000.0} {timestamp}");
            
            lines.Add("# TYPE process_threads gauge");
            lines.Add($"process_threads {system.ThreadCount} {timestamp}");
        }

        // Add request metrics
        if (metrics.ContainsKey("requests"))
        {
            var requests = (object[])metrics["requests"];
            foreach (dynamic request in requests)
            {
                var labels = $"method=\"{request.Method}\",endpoint=\"{request.Endpoint}\"";
                
                lines.Add("# TYPE http_requests_total counter");
                lines.Add($"http_requests_total{{{labels}}} {request.TotalRequests} {timestamp}");
                
                lines.Add("# TYPE http_request_duration_seconds gauge");
                lines.Add($"http_request_duration_seconds{{{labels}}} {request.AverageDuration / 1000.0} {timestamp}");
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    private static string SanitizeMetricName(string name)
    {
        // Replace invalid characters for Prometheus metric names
        return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_:]", "_")
            .ToLowerInvariant();
    }
}