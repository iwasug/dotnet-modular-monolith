namespace ModularMonolith.Authentication.Services;

/// <summary>
/// Localization service specifically for the Authentication module
/// </summary>
public interface IAuthLocalizationService
{
    /// <summary>
    /// Gets a localized string from Authentication module resources
    /// </summary>
    string GetString(string key, string? culture = null);

    /// <summary>
    /// Gets a localized string with format arguments from Authentication module resources
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string with culture and format arguments from Authentication module resources
    /// </summary>
    string GetString(string key, string? culture, params object[] args);
}