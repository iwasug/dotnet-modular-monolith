namespace ModularMonolith.Roles.Services;

/// <summary>
/// Localization service specifically for the Roles module
/// </summary>
public interface IRoleLocalizationService
{
    /// <summary>
    /// Gets a localized string from Roles module resources
    /// </summary>
    string GetString(string key, string? culture = null);

    /// <summary>
    /// Gets a localized string with format arguments from Roles module resources
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string with culture and format arguments from Roles module resources
    /// </summary>
    string GetString(string key, string? culture, params object[] args);
}