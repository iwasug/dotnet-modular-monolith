using Serilog;
using Serilog.Events;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring Serilog structured logging
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with structured logging, correlation IDs, and multiple sinks
    /// </summary>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("ProcessId", Environment.ProcessId)
            .Enrich.WithProperty("Application", "ModularMonolith")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    // PostgreSQL column options can be configured when the PostgreSQL sink is properly set up

    /// <summary>
    /// Adds request logging middleware with structured logging
    /// </summary>
    public static void UseStructuredRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.GetLevel = GetLogLevel;
            options.EnrichDiagnosticContext = EnrichFromRequest;
        });
    }

    /// <summary>
    /// Determines log level based on HTTP status code and response time
    /// </summary>
    private static LogEventLevel GetLogLevel(HttpContext ctx, double _, Exception? ex) =>
        ex is not null
            ? LogEventLevel.Error
            : ctx.Response.StatusCode > 499
                ? LogEventLevel.Error
                : ctx.Response.StatusCode > 399
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;

    /// <summary>
    /// Enriches log context with request information
    /// </summary>
    private static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        var user = httpContext.User;

        diagnosticContext.Set("RequestHost", request.Host.Value ?? "unknown");
        diagnosticContext.Set("RequestScheme", request.Scheme ?? "unknown");
        diagnosticContext.Set("RequestProtocol", request.Protocol ?? "unknown");
        diagnosticContext.Set("RequestMethod", request.Method ?? "unknown");
        diagnosticContext.Set("RequestPath", request.Path.Value ?? "unknown");
        diagnosticContext.Set("RequestQueryString", request.QueryString.Value ?? "");
        diagnosticContext.Set("RequestContentType", request.ContentType ?? "");
        diagnosticContext.Set("RequestContentLength", request.ContentLength ?? 0);
        diagnosticContext.Set("ResponseStatusCode", response.StatusCode);
        diagnosticContext.Set("ResponseContentType", response.ContentType ?? "");
        diagnosticContext.Set("ResponseContentLength", response.ContentLength ?? 0);

        // Add user information if authenticated
        if (user.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", user.FindFirst("sub")?.Value ?? user.FindFirst("id")?.Value ?? "unknown");
            diagnosticContext.Set("UserEmail", user.FindFirst("email")?.Value ?? "unknown");
            diagnosticContext.Set("UserRoles", string.Join(",", user.FindAll("role").Select(c => c.Value)));
        }

        // Add client information
        diagnosticContext.Set("ClientIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        diagnosticContext.Set("UserAgent", request.Headers.UserAgent.ToString());
        
        // Add correlation ID if present
        if (request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId.ToString());
        }
    }
}