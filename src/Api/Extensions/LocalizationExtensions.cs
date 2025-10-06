using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring localization services
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Adds comprehensive localization services to the service collection
    /// </summary>
    public static IServiceCollection AddComprehensiveLocalization(this IServiceCollection services)
    {
        // Add localization services
        services.AddLocalization(options =>
        {
            options.ResourcesPath = "Resources";
        });

        // Configure supported cultures
        var supportedCultures = new[]
        {
            new CultureInfo("en-US"), // Default culture
            new CultureInfo("es-ES"), // Spanish
            new CultureInfo("fr-FR"), // French
            new CultureInfo("de-DE"), // German
            new CultureInfo("pt-BR"), // Portuguese (Brazil)
            new CultureInfo("it-IT"), // Italian
            new CultureInfo("ja-JP"), // Japanese
            new CultureInfo("zh-CN"), // Chinese (Simplified)
            new CultureInfo("id-ID")  // Indonesian
        };

        // Configure request localization options
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            // Configure culture providers in order of preference
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                // 1. Accept-Language header (primary)
                new AcceptLanguageHeaderRequestCultureProvider(),
                
                // 2. Query string parameter (?culture=en-US)
                new QueryStringRequestCultureProvider(),
                
                // 3. Cookie (for user preference persistence)
                new CookieRequestCultureProvider
                {
                    CookieName = "UserCulture"
                }
            };

            // Fallback to default culture if none match
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
        });

        return services;
    }

    /// <summary>
    /// Configures the application to use comprehensive localization middleware
    /// </summary>
    public static IApplicationBuilder UseComprehensiveLocalization(this IApplicationBuilder app)
    {
        // Use request localization middleware
        app.UseRequestLocalization();

        return app;
    }

    /// <summary>
    /// Gets the current culture from the HTTP context
    /// </summary>
    public static string GetCurrentCulture(this HttpContext httpContext)
    {
        return CultureInfo.CurrentUICulture.Name;
    }

    /// <summary>
    /// Gets the current culture from the HTTP context with fallback
    /// </summary>
    public static string GetCurrentCultureWithFallback(this HttpContext httpContext, string fallback = "en-US")
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        return string.IsNullOrEmpty(culture) ? fallback : culture;
    }

    /// <summary>
    /// Determines if the current culture is right-to-left
    /// </summary>
    public static bool IsRightToLeft(this HttpContext httpContext)
    {
        return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
    }

    /// <summary>
    /// Gets supported cultures for API responses
    /// </summary>
    public static string[] GetSupportedCultures()
    {
        return new[]
        {
            "en-US", "es-ES", "fr-FR", "de-DE",
            "pt-BR", "it-IT", "ja-JP", "zh-CN", "id-ID"
        };
    }

    /// <summary>
    /// Validates if a culture is supported
    /// </summary>
    public static bool IsCultureSupported(string culture)
    {
        return GetSupportedCultures().Contains(culture, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds localized validation middleware to handle FluentValidation exceptions
    /// </summary>
    public static IApplicationBuilder UseLocalizedValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<Middleware.LocalizedValidationMiddleware>();
    }
}