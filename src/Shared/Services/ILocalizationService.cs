using System.Globalization;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Service for providing localized strings across the application
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized string or key if not found</returns>
    string GetString(string key, string? culture = null);

    /// <summary>
    /// Gets a localized string by key with format arguments
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Formatted localized string</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string by key with culture and format arguments
    /// </summary>
    /// <param name="key">The resource key</param>
    /// <param name="culture">Culture to use</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Formatted localized string</returns>
    string GetString(string key, string? culture, params object[] args);

    /// <summary>
    /// Gets the current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the current UI culture
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets all supported cultures
    /// </summary>
    string[] SupportedCultures { get; }

    /// <summary>
    /// Checks if a culture is supported
    /// </summary>
    /// <param name="culture">Culture to check</param>
    /// <returns>True if supported, false otherwise</returns>
    bool IsCultureSupported(string culture);
}