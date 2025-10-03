namespace ModularMonolith.Users.Services;

/// <summary>
/// Localization service specifically for the Users module
/// </summary>
public interface IUserLocalizationService
{
    /// <summary>
    /// Gets a localized string from Users module resources
    /// </summary>
    string GetString(string key, string? culture = null);

    /// <summary>
    /// Gets a localized string with format arguments from Users module resources
    /// </summary>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string with culture and format arguments from Users module resources
    /// </summary>
    string GetString(string key, string? culture, params object[] args);
}