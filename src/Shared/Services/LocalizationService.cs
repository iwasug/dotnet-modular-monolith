using System.Globalization;
using System.Resources;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Implementation of localization service for shared resources
/// </summary>
public sealed class LocalizationService(ILogger<LocalizationService> logger) : ILocalizationService
{
    private readonly ResourceManager _validationResourceManager = new(
        "ModularMonolith.Shared.Resources.ValidationMessages", 
        typeof(LocalizationService).Assembly);
    private readonly ResourceManager _errorResourceManager = new(
        "ModularMonolith.Shared.Resources.ErrorMessages", 
        typeof(LocalizationService).Assembly);

    private readonly string[] _supportedCultures = new[]
    {
        "en-US", "es-ES", "fr-FR", "de-DE",
        "pt-BR", "it-IT", "ja-JP", "zh-CN"
    };

    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

    public string[] SupportedCultures => _supportedCultures;

    public string GetString(string key, string? culture = null)
    {
        try
        {
            var cultureInfo = GetCultureInfo(culture);
            
            // Try validation messages first, then error messages
            var value = _validationResourceManager.GetString(key, cultureInfo) 
                       ?? _errorResourceManager.GetString(key, cultureInfo);
            
            if (value is null)
            {
                logger.LogWarning("Localization key '{Key}' not found for culture '{Culture}'", key, cultureInfo.Name);
                return key; // Return key as fallback
            }

            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting localized string for key '{Key}' and culture '{Culture}'", key, culture);
            return key; // Return key as fallback
        }
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error formatting localized string for key '{Key}' with args", key);
            return key; // Return key as fallback
        }
    }

    public bool IsCultureSupported(string culture)
    {
        return _supportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase);
    }

    private CultureInfo GetCultureInfo(string? culture)
    {
        if (string.IsNullOrEmpty(culture))
        {
            return CultureInfo.CurrentUICulture;
        }

        try
        {
            return new CultureInfo(culture);
        }
        catch (CultureNotFoundException)
        {
            logger.LogWarning("Culture '{Culture}' not found, falling back to current UI culture", culture);
            return CultureInfo.CurrentUICulture;
        }
    }
}