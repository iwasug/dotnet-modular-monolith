using ModularMonolith.Shared.Services;

namespace ModularMonolith.Users.Services;

/// <summary>
/// Implementation of localization service for the Users module
/// </summary>
public sealed class UserLocalizationService(IModularLocalizationService modularLocalizationService)
    : IUserLocalizationService
{
    private const string ModuleName = "Users";

    public string GetString(string key, string? culture = null)
    {
        return modularLocalizationService.GetModuleString(ModuleName, key, culture);
    }

    public string GetString(string key, params object[] args)
    {
        return GetString(key, null, args);
    }

    public string GetString(string key, string? culture, params object[] args)
    {
        try
        {
            var format = GetString(key, culture);
            return string.Format(format, args);
        }
        catch
        {
            return key; // Return key as fallback
        }
    }
}