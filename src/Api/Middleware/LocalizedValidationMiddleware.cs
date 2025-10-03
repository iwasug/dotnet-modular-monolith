using System.Net;
using System.Text.Json;
using FluentValidation;
using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Services;
using ModularMonolith.Api.Extensions;

namespace ModularMonolith.Api.Middleware;

/// <summary>
/// Middleware to handle FluentValidation exceptions with localized error messages
/// </summary>
internal sealed class LocalizedValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LocalizedValidationMiddleware> _logger;
    private readonly ILocalizedErrorService _localizedErrorService;

    public LocalizedValidationMiddleware(
        RequestDelegate next,
        ILogger<LocalizedValidationMiddleware> logger,
        ILocalizedErrorService localizedErrorService)
    {
        _next = next;
        _logger = logger;
        _localizedErrorService = localizedErrorService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            await HandleValidationExceptionAsync(context, validationException);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException validationException)
    {
        var correlationId = context.TraceIdentifier;
        var culture = context.GetCurrentCultureWithFallback();

        _logger.LogWarning(
            "Validation failed for request {RequestMethod} {RequestPath} with {ErrorCount} errors",
            context.Request.Method,
            context.Request.Path,
            validationException.Errors.Count());

        // Create localized validation error response
        var validationErrors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        var error = _localizedErrorService.CreateValidationError(
            "VALIDATION_FAILED", 
            "ValidationFailed", 
            culture);

        var errorResponse = new ValidationErrorResponse
        {
            Error = error,
            ValidationErrors = validationErrors,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Validation error response format with localized messages
/// </summary>
internal sealed record ValidationErrorResponse
{
    public required Error Error { get; init; }
    public required IDictionary<string, string[]> ValidationErrors { get; init; }
    public required string CorrelationId { get; init; }
    public required DateTime Timestamp { get; init; }
}