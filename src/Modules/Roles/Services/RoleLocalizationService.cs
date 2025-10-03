using ModularMonolith.Shared.Services;

namespace ModularMonolith.Roles.Services;

/// <summary>
/// Implementation of localization service for the Roles module
/// </summary>
public sealed class RoleLocalizationService : IRoleLocalizationService
{
    private readonly IModularLocalizationService _modularLocalizationService;
    private const string ModuleName = "Roles";

    public RoleLocalizationService(IModularLocalizationService modularLocalizationService)
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