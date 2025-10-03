using ModularMonolith.Shared.Services;

namespace ModularMonolith.Authentication.Services;

/// <summary>
/// Implementation of localization service for the Authentication module
/// </summary>
public sealed class AuthLocalizationService : IAuthLocalizationService
{
    private readonly IModularLocalizationService _modularLocalizationService;
    private const string ModuleName = "Authentication";

    public AuthLocalizationService(IModularLocalizationService modularLocalizationService)
    {
        _modularLocalizationService = modularLocalizationService;
    }

    public string GetString(string key, string? culture = null)
    {
        return _modularLocalizationService.GetModuleString(ModuleName, key, culture);
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