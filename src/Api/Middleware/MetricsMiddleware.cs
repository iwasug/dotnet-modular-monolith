using ModularMonolith.Api.Services;
using System.Diagnostics;

namespace ModularMonolith.Api.Middleware;

/// <summary>
/// Middleware to collect performance metrics for HTTP requests
/// </summary>
public sealed class MetricsMiddleware(RequestDelegate next, IMetricsService metricsService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        
        // Record request start
        metricsService.IncrementCounter("http_requests_total", new Dictionary<string, string>
        {
            ["method"] = request.Method,
            ["endpoint"] = GetEndpointPattern(context)
        });

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var response = context.Response;
            var endpoint = GetEndpointPattern(context);
            
            // Record request duration and status
            metricsService.RecordRequestDuration(
                endpoint, 
                request.Method, 
                response.StatusCode, 
                stopwatch.Elapsed.TotalMilliseconds);

            // Record response status code counter
            metricsService.IncrementCounter("http_responses_total", new Dictionary<string, string>
            {
                ["method"] = request.Method,
                ["endpoint"] = endpoint,
                ["status_code"] = response.StatusCode.ToString(),
                ["status_class"] = GetStatusClass(response.StatusCode)
            });

            // Record request duration histogram
            metricsService.RecordHistogram("http_request_duration_ms", 
                stopwatch.Elapsed.TotalMilliseconds, 
                new Dictionary<string, string>
                {
                    ["method"] = request.Method,
                    ["endpoint"] = endpoint
                });

            // Record response size if available
            if (response.ContentLength.HasValue)
            {
                metricsService.RecordHistogram("http_response_size_bytes", 
                    response.ContentLength.Value,
                    new Dictionary<string, string>
                    {
                        ["method"] = request.Method,
                        ["endpoint"] = endpoint
                    });
            }

            // Record concurrent requests gauge
            var activeRequests = GetActiveRequestCount(context);
            metricsService.RecordGauge("http_requests_active", activeRequests);
        }
    }

    private static string GetEndpointPattern(HttpContext context)
    {
        // Try to get the route pattern from endpoint metadata
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.RouteEndpoint>() is { } routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "unknown";
        }

        // Fallback to path with parameter normalization
        var path = context.Request.Path.Value ?? "unknown";
        
        // Normalize common patterns to reduce cardinality
        path = System.Text.RegularExpressions.Regex.Replace(path, @"/\d+", "/{id}");
        path = System.Text.RegularExpressions.Regex.Replace(path, @"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "/{guid}");
        
        return path;
    }

    private static string GetStatusClass(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "2xx",
            >= 300 and < 400 => "3xx",
            >= 400 and < 500 => "4xx",
            >= 500 => "5xx",
            _ => "1xx"
        };
    }

    private static int GetActiveRequestCount(HttpContext context)
    {
        // This is a simplified implementation
        // In a real-world scenario, you might want to use a more sophisticated approach
        // to track active requests across the application
        return 1; // Placeholder - would need proper implementation with shared counter
    }
}