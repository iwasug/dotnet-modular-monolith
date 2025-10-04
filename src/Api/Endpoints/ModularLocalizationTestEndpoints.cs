using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Shared.Services;
using ModularMonolith.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Test endpoints for modular localization functionality
/// </summary>
public sealed class ModularLocalizationTestEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication endpoints)
    {
        var modularTest = endpoints.MapGroup("/api/modular-localization-test")
            .WithTags("Modular Localization Test")
            .WithOpenApi();

        // Test module-specific resources
        modularTest.MapGet("/module/{moduleName}/messages", TestModuleMessages)
            .WithName("TestModuleMessages")
            .WithSummary("Test module-specific localization messages")
            .WithDescription("Tests localization message retrieval for a specific module")
            .Produces<ModuleMessageTestResponse>()
            .Produces<ProblemDetails>(400);

        // Test all modules
        modularTest.MapGet("/all-modules", TestAllModules)
            .WithName("TestAllModules")
            .WithSummary("Test all module localizations")
            .WithDescription("Tests localization for all modules")
            .Produces<AllModulesTestResponse>();

        // Test module structure
        modularTest.MapGet("/module-structure", GetModuleStructure)
            .WithName("GetModuleStructure")
            .WithSummary("Get modular localization structure")
            .WithDescription("Returns the structure of modular localization system")
            .Produces<ModuleStructureResponse>();

        // Test specific module with culture
        modularTest.MapGet("/module/{moduleName}/culture/{culture}", TestModuleWithCulture)
            .WithName("TestModuleWithCulture")
            .WithSummary("Test module messages with specific culture")
            .WithDescription("Tests module localization with specific culture")
            .Produces<ModuleCultureTestResponse>()
            .Produces<ProblemDetails>(400);
    }

    private static IResult TestModuleMessages(
        string moduleName,
        HttpContext context,
        IModularLocalizationService modularLocalizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        
        var testKeys = GetTestKeysForModule(moduleName);
        if (testKeys.Length == 0)
        {
            return Results.Problem(
                title: "Unknown module",
                detail: $"Module '{moduleName}' is not recognized or has no test keys",
                statusCode: 400);
        }

        var messages = new Dictionary<string, string>();
        foreach (var key in testKeys)
        {
            messages[key] = modularLocalizationService.GetModuleString(moduleName, key, culture);
        }

        return Results.Ok(new ModuleMessageTestResponse(moduleName, culture, messages));
    }

    private static IResult TestAllModules(
        HttpContext context,
        IModularLocalizationService modularLocalizationService)
    {
        var culture = context.GetCurrentCultureWithFallback();
        var modules = new[] { "Users", "Roles", "Authentication", "Api", "Shared" };
        
        var moduleResults = new Dictionary<string, Dictionary<string, string>>();
        
        foreach (var module in modules)
        {
            var testKeys = GetTestKeysForModule(module);
            var messages = new Dictionary<string, string>();
            
            foreach (var key in testKeys)
            {
                messages[key] = modularLocalizationService.GetModuleString(module, key, culture);
            }
            
            moduleResults[module] = messages;
        }

        return Results.Ok(new AllModulesTestResponse(culture, moduleResults));
    }

    private static IResult GetModuleStructure()
    {
        var structure = new Dictionary<string, object>
        {
            ["Users"] = new
            {
                ResourcePath = "src/Modules/Users/Resources",
                ResourceFiles = new[] { "user-messages" },
                SampleKeys = new[] { "EmailRequired", "UserNotFound", "UserAlreadyExists" }
            },
            ["Roles"] = new
            {
                ResourcePath = "src/Modules/Roles/Resources",
                ResourceFiles = new[] { "role-messages" },
                SampleKeys = new[] { "RoleNameRequired", "RoleNotFound", "RoleAlreadyExists" }
            },
            ["Authentication"] = new
            {
                ResourcePath = "src/Modules/Authentication/Resources",
                ResourceFiles = new[] { "auth-messages" },
                SampleKeys = new[] { "InvalidCredentials", "TokenExpired", "AccessDenied" }
            },
            ["Api"] = new
            {
                ResourcePath = "src/Api/Resources",
                ResourceFiles = new[] { "api-documentation" },
                SampleKeys = new[] { "ApiTitle", "ApiDescription", "AuthenticationDescription" }
            },
            ["Shared"] = new
            {
                ResourcePath = "src/Shared/Resources",
                ResourceFiles = new[] { "validation-messages", "error-messages" },
                SampleKeys = new[] { "Required", "InvalidFormat", "ValidationFailed" }
            }
        };

        return Results.Ok(new ModuleStructureResponse(structure));
    }

    private static IResult TestModuleWithCulture(
        string moduleName,
        string culture,
        IModularLocalizationService modularLocalizationService)
    {
        if (!modularLocalizationService.IsCultureSupported(culture))
        {
            return Results.Problem(
                title: "Unsupported culture",
                detail: $"Culture '{culture}' is not supported",
                statusCode: 400);
        }

        var testKeys = GetTestKeysForModule(moduleName);
        if (testKeys.Length == 0)
        {
            return Results.Problem(
                title: "Unknown module",
                detail: $"Module '{moduleName}' is not recognized",
                statusCode: 400);
        }

        var messages = new Dictionary<string, string>();
        foreach (var key in testKeys)
        {
            messages[key] = modularLocalizationService.GetModuleString(moduleName, key, culture);
        }

        return Results.Ok(new ModuleCultureTestResponse(moduleName, culture, messages));
    }

    private static string[] GetTestKeysForModule(string moduleName)
    {
        return moduleName.ToLower() switch
        {
            "users" => new[] { "EmailRequired", "UserNotFound", "UserAlreadyExists", "PasswordRequired" },
            "roles" => new[] { "RoleNameRequired", "RoleNotFound", "RoleAlreadyExists", "PermissionsRequired" },
            "authentication" => new[] { "InvalidCredentials", "TokenExpired", "AccessDenied", "LoginFailed" },
            "api" => new[] { "ApiTitle", "ApiDescription", "AuthenticationDescription" },
            "shared" => new[] { "Required", "InvalidFormat", "ValidationFailed", "NotFound" },
            _ => Array.Empty<string>()
        };
    }
}

/// <summary>
/// Response for module message test
/// </summary>
public sealed record ModuleMessageTestResponse(string ModuleName, string Culture, Dictionary<string, string> Messages);

/// <summary>
/// Response for all modules test
/// </summary>
public sealed record AllModulesTestResponse(string Culture, Dictionary<string, Dictionary<string, string>> ModuleResults);

/// <summary>
/// Response for module structure
/// </summary>
public sealed record ModuleStructureResponse(Dictionary<string, object> Structure);

/// <summary>
/// Response for module culture test
/// </summary>
public sealed record ModuleCultureTestResponse(string ModuleName, string Culture, Dictionary<string, string> Messages);