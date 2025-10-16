using System.Net;
using System.Text.Json;
using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Services;
using ModularMonolith.Api.Extensions;

namespace ModularMonolith.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions
/// and returns properly formatted error responses with structured logging
/// </summary>
internal sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment environment,
    ILocalizedErrorService localizedErrorService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        // Log the exception with structured logging
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method,
            ["UserId"] = context.User?.Identity?.Name ?? "Anonymous",
            ["UserAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["RemoteIpAddress"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        });

        logger.LogError(exception, 
            "Unhandled exception occurred while processing request {RequestMethod} {RequestPath}",
            context.Request.Method, 
            context.Request.Path);

        // Get current culture for localized error messages
        var culture = context.GetCurrentCultureWithFallback();
        
        // Determine the appropriate HTTP status code and error details
        var (statusCode, error) = MapExceptionToError(exception, culture);

        // Create error response
        var errorResponse = new ErrorResponse
        {
            Error = error,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        // Set response properties
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Serialize and write the error response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private (HttpStatusCode StatusCode, Error Error) MapExceptionToError(Exception exception, string culture)
    {
        return exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, 
                localizedErrorService.CreateValidationError("MISSING_ARGUMENT", "BadRequest", culture)),
            
            ArgumentException => (HttpStatusCode.BadRequest, 
                localizedErrorService.CreateValidationError("INVALID_ARGUMENT", "BadRequest", culture)),
            
            InvalidOperationException => (HttpStatusCode.BadRequest, 
                localizedErrorService.CreateValidationError("INVALID_OPERATION", "BadRequest", culture)),
            
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, 
                localizedErrorService.CreateUnauthorizedError("UNAUTHORIZED_ACCESS", "UnauthorizedAccess", culture)),
            
            NotImplementedException => (HttpStatusCode.NotImplemented, 
                localizedErrorService.CreateInternalError("NOT_IMPLEMENTED", "InternalServerError", culture)),
            
            TimeoutException => (HttpStatusCode.RequestTimeout, 
                localizedErrorService.CreateInternalError("REQUEST_TIMEOUT", "InternalServerError", culture)),
            
            TaskCanceledException => (HttpStatusCode.RequestTimeout, 
                localizedErrorService.CreateInternalError("REQUEST_CANCELLED", "InternalServerError", culture)),
            
            _ => (HttpStatusCode.InternalServerError, 
                environment.IsDevelopment() 
                    ? Error.Internal("INTERNAL_ERROR", exception.Message)
                    : localizedErrorService.CreateInternalError("INTERNAL_ERROR", "InternalServerError", culture))
        };
    }
}

/// <summary>
/// Standardized error response format
/// </summary>
internal sealed record ErrorResponse
{
    public required Error Error { get; init; }
    public required string CorrelationId { get; init; }
    public required DateTime Timestamp { get; init; }
}