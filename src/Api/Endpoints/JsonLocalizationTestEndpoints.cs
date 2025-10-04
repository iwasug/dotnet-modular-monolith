using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Shared.Services;
using ModularMonolith.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Test endpoints specifically for JSON-based localization functionality
/// </summary>
public sealed class JsonLocalizationTestEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication endpoints)
    {
        var localizationTest = endpoints.MapGroup("/api/localization-test")
            .WithTags("Localization Test")
            .WithOpenApi();

        // Test JSON resource loading
        localizationTest.MapGet("/json-resources", TestJsonResources)
            .WithName("TestJsonResources")
            .WithSummary("Test JSON resource loading")
            .WithDescription("Tests if JSON localization resources are loaded correctly")
            .Produces<JsonResourceTestResponse>();

        // Test validation messages
        localizationTest.MapGet("/validation-messages", TestValidationMessages)
            .WithName("TestValidationMessages")
            .WithSummary("Test validation message localization")
            .WithDescription("Tests validation message retrieval in different languages")
            .Produces<ValidationMessageTestResponse>();

        // Test error messages
        localizationTest.MapGet("/error-messages", TestErrorMessages)
            .WithName("TestErrorMessages")
            .WithSummary("Test error message localization")
            .WithDescription("Tests error message retrieval in different languages")
            .Produces<ErrorMessageTestResponse>();

        // Test culture-specific messages
        localizationTest.MapGet("/culture-test/{culture}", TestCultureSpecificMessages)
            .WithName("TestCultureSpecificMessages")
            .WithSummary("Test messages for specific culture")
            .WithDescription("Tests message retrieval for a specific culture")
            .Produces<CultureTestResponse>()
            .Produces<ProblemDetails>(400);
    }

    private static IResult TestJsonResources(
        HttpContext context,
        ILocalizationService localizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        
        var testResults = new Dictionary<string, string>
        {
            ["EmailRequired"] = localizationService.GetString("EmailRequired", culture),
            ["PasswordRequired"] = localizationService.GetString("PasswordRequired", culture),
            ["UserNotFound"] = localizationService.GetString("UserNotFound", culture),
            ["ValidationFailed"] = localizationService.GetString("ValidationFailed", culture),
            ["ApiTitle"] = localizationService.GetString("ApiTitle", culture)
        };

        return Results.Ok(new JsonResourceTestResponse(culture, testResults));
    }

    private static IResult TestValidationMessages(
        HttpContext context,
        ILocalizationService localizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        
        var validationMessages = new Dictionary<string, string>
        {
            ["EmailRequired"] = localizationService.GetString("EmailRequired", culture),
            ["EmailInvalid"] = localizationService.GetString("EmailInvalid", culture),
            ["PasswordRequired"] = localizationService.GetString("PasswordRequired", culture),
            ["PasswordMinLength"] = localizationService.GetString("PasswordMinLength", culture),
            ["FirstNameRequired"] = localizationService.GetString("FirstNameRequired", culture),
            ["LastNameRequired"] = localizationService.GetString("LastNameRequired", culture)
        };

        return Results.Ok(new ValidationMessageTestResponse(culture, validationMessages));
    }

    private static IResult TestErrorMessages(
        HttpContext context,
        ILocalizationService localizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        
        var errorMessages = new Dictionary<string, string>
        {
            ["UserNotFound"] = localizationService.GetString("UserNotFound", culture),
            ["UserAlreadyExists"] = localizationService.GetString("UserAlreadyExists", culture),
            ["InvalidCredentials"] = localizationService.GetString("InvalidCredentials", culture),
            ["AccessDenied"] = localizationService.GetString("AccessDenied", culture),
            ["ValidationFailed"] = localizationService.GetString("ValidationFailed", culture),
            ["InternalServerError"] = localizationService.GetString("InternalServerError", culture)
        };

        return Results.Ok(new ErrorMessageTestResponse(culture, errorMessages));
    }

    private static IResult TestCultureSpecificMessages(
        string culture,
        ILocalizationService localizationService)
    {
        if (!localizationService.IsCultureSupported(culture))
        {
            return Results.Problem(
                title: "Unsupported culture",
                detail: $"Culture '{culture}' is not supported",
                statusCode: 400);
        }

        var messages = new Dictionary<string, string>
        {
            ["EmailRequired"] = localizationService.GetString("EmailRequired", culture),
            ["UserNotFound"] = localizationService.GetString("UserNotFound", culture),
            ["ApiTitle"] = localizationService.GetString("ApiTitle", culture),
            ["ValidationFailed"] = localizationService.GetString("ValidationFailed", culture)
        };

        return Results.Ok(new CultureTestResponse(culture, messages));
    }
}

/// <summary>
/// Response for JSON resource test
/// </summary>
public sealed record JsonResourceTestResponse(string Culture, Dictionary<string, string> Resources);

/// <summary>
/// Response for validation message test
/// </summary>
public sealed record ValidationMessageTestResponse(string Culture, Dictionary<string, string> ValidationMessages);

/// <summary>
/// Response for error message test
/// </summary>
public sealed record ErrorMessageTestResponse(string Culture, Dictionary<string, string> ErrorMessages);

/// <summary>
/// Response for culture-specific test
/// </summary>
public sealed record CultureTestResponse(string Culture, Dictionary<string, string> Messages);