namespace ModularMonolith.Shared.Services;

/// <summary>
/// Extended localization service interface that supports module-specific resource access
/// </summary>
public interface IModularLocalizationService : ILocalizationService
{
    /// <summary>
    /// Gets a localized string specifically from a module's resources
    /// </summary>
    /// <param name="moduleName">Name of the module (Users, Roles, Authentication, etc.)</param>
    /// <param name="key">The resource key</param>
    /// <param name="culture">Optional culture, uses current culture if not specified</param>
    /// <returns>Localized string or key if not found</returns>
    string GetModuleString(string moduleName, string key, string? culture = null);
}