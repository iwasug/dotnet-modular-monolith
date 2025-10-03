namespace ModularMonolith.Api.Services;

/// <summary>
/// Service for providing localized API documentation content
/// </summary>
public interface IApiDocumentationService
{
    /// <summary>
    /// Gets the localized API title
    /// </summary>
    /// <param name="culture">The culture code (e.g., "en-US", "es-ES")</param>
    /// <returns>The localized API title</returns>
    string GetApiTitle(string? culture = null);

    /// <summary>
    /// Gets the localized API description
    /// </summary>
    /// <param name="culture">The culture code (e.g., "en-US", "es-ES")</param>
    /// <returns>The localized API description</returns>
    string GetApiDescription(string? culture = null);

    /// <summary>
    /// Gets the localized authentication description
    /// </summary>
    /// <param name="culture">The culture code (e.g., "en-US", "es-ES")</param>
    /// <returns>The localized authentication description</returns>
    string GetAuthenticationDescription(string? culture = null);

    /// <summary>
    /// Gets a localized error message
    /// </summary>
    /// <param name="errorType">The type of error</param>
    /// <param name="culture">The culture code (e.g., "en-US", "es-ES")</param>
    /// <returns>The localized error message</returns>
    string GetErrorMessage(string errorType, string? culture = null);

    /// <summary>
    /// Gets all supported cultures
    /// </summary>
    /// <returns>Array of supported culture codes</returns>
    string[] GetSupportedCultures();
}