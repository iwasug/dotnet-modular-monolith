using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Shared.Services;
using ModularMonolith.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Endpoints for testing and managing localization functionality
/// </summary>
public sealed class LocalizationEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication endpoints)
    {
        var localization = endpoints.MapGroup("/api/localization")
            .WithTags("Localization")
            .WithOpenApi();

        // Get supported cultures
        localization.MapGet("/cultures", GetSupportedCultures)
            .WithName("GetSupportedCultures")
            .WithSummary("Get all supported cultures")
            .WithDescription("Returns a list of all supported culture codes for localization")
            .Produces<SupportedCulturesResponse>();

        // Get current culture
        localization.MapGet("/current-culture", GetCurrentCulture)
            .WithName("GetCurrentCulture")
            .WithSummary("Get current request culture")
            .WithDescription("Returns the current culture determined from the Accept-Language header")
            .Produces<CurrentCultureResponse>();

        // Test localized message
        localization.MapGet("/test-message/{key}", GetLocalizedMessage)
            .WithName("GetLocalizedMessage")
            .WithSummary("Test localized message retrieval")
            .WithDescription("Returns a localized message for the given key in the current culture")
            .Produces<LocalizedMessageResponse>()
            .Produces<ProblemDetails>(404);

        // Test validation messages
        localization.MapPost("/test-validation", TestValidationMessages)
            .WithName("TestValidationMessages")
            .WithSummary("Test localized validation messages")
            .WithDescription("Tests validation with localized error messages")
            .Produces<ValidationTestResponse>()
            .ProducesValidationProblem();
    }

    private static IResult GetSupportedCultures()
    {
        var cultures = LocalizationExtensions.GetSupportedCultures();
        return Results.Ok(new SupportedCulturesResponse(cultures));
    }

    private static IResult GetCurrentCulture(HttpContext context)
    {
        var culture = context.GetCurrentCultureWithFallback();
        var isRtl = context.IsRightToLeft();
        
        return Results.Ok(new CurrentCultureResponse(culture, isRtl));
    }

    private static IResult GetLocalizedMessage(
        string key,
        HttpContext context,
        ILocalizationService localizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        var message = localizationService.GetString(key, culture);
        
        if (message == key) // Key not found
        {
            return Results.Problem(
                title: "Localization key not found",
                detail: $"The localization key '{key}' was not found for culture '{culture}'",
                statusCode: 404);
        }

        return Results.Ok(new LocalizedMessageResponse(key, message, culture));
    }

    private static IResult TestValidationMessages(
        ValidationTestRequest request,
        ILocalizationService localizationService)
    {
        var errors = new List<string>();

        // Test various validation scenarios
        if (string.IsNullOrEmpty(request.Email))
        {
            errors.Add(localizationService.GetString("EmailRequired"));
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add(localizationService.GetString("EmailInvalid"));
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add(localizationService.GetString("PasswordRequired"));
        }
        else if (request.Password.Length < 8)
        {
            errors.Add(localizationService.GetString("PasswordMinLength"));
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors.ToDictionary(e => "ValidationErrors", e => new[] { e }));
        }

        return Results.Ok(new ValidationTestResponse("Validation passed", errors));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Response containing supported cultures
/// </summary>
public sealed record SupportedCulturesResponse(string[] Cultures);

/// <summary>
/// Response containing current culture information
/// </summary>
public sealed record CurrentCultureResponse(string Culture, bool IsRightToLeft);

/// <summary>
/// Response containing a localized message
/// </summary>
public sealed record LocalizedMessageResponse(string Key, string Message, string Culture);

/// <summary>
/// Request for testing validation messages
/// </summary>
public sealed record ValidationTestRequest(string? Email, string? Password);

/// <summary>
/// Response for validation test
/// </summary>
public sealed record ValidationTestResponse(string Message, List<string> Errors);