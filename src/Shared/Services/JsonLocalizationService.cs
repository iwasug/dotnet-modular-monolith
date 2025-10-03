using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// JSON-based implementation of localization service
/// </summary>
public sealed class JsonLocalizationService : ILocalizationService
{
    private readonly ILogger<JsonLocalizationService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string[] _supportedCultures;
    private readonly Dictionary<string, Dictionary<string, string>> _resourceCache;
    private readonly object _cacheLock = new();

    public JsonLocalizationService(ILogger<JsonLocalizationService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _supportedCultures = new[]
        {
            "en-US", "es-ES", "fr-FR", "de-DE",
            "pt-BR", "it-IT", "ja-JP", "zh-CN", "id-ID"
        };
        _resourceCache = new Dictionary<string, Dictionary<string, string>>();
        
        // Pre-load resources
        LoadAllResources();
    }

    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

    public string[] SupportedCultures => _supportedCultures;

    public string GetString(string key, string? culture = null)
    {
        try
        {
            var cultureInfo = GetCultureInfo(culture);
            var cultureKey = cultureInfo.Name;
            
            // Try to get from cache
            var resources = GetResourcesForCulture(cultureKey);
            
            if (resources.TryGetValue(key, out var value))
            {
                return value;
            }

            // Try fallback to default culture if not found
            if (cultureKey != "en-US")
            {
                var defaultResources = GetResourcesForCulture("en-US");
                if (defaultResources.TryGetValue(key, out var fallbackValue))
                {
                    return fallbackValue;
                }
            }

            _logger.LogWarning("Localization key '{Key}' not found for culture '{Culture}'", key, cultureKey);
            return key; // Return key as fallback
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized string for key '{Key}' and culture '{Culture}'", key, culture);
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
            _logger.LogError(ex, "Error formatting localized string for key '{Key}' with args", key);
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
            _logger.LogWarning("Culture '{Culture}' not found, falling back to current UI culture", culture);
            return CultureInfo.CurrentUICulture;
        }
    }

    private Dictionary<string, string> GetResourcesForCulture(string culture)
    {
        lock (_cacheLock)
        {
            if (_resourceCache.TryGetValue(culture, out var cachedResources))
            {
                return cachedResources;
            }

            // Load resources for this culture
            var resources = LoadResourcesForCulture(culture);
            _resourceCache[culture] = resources;
            return resources;
        }
    }

    private Dictionary<string, string> LoadResourcesForCulture(string culture)
    {
        var resources = new Dictionary<string, string>();
        
        // Load validation messages
        LoadJsonResource(resources, "validation-messages", culture);
        
        // Load error messages
        LoadJsonResource(resources, "error-messages", culture);
        
        // Load API documentation (from API project)
        LoadApiJsonResource(resources, "api-documentation", culture);

        return resources;
    }

    private void LoadJsonResource(Dictionary<string, string> resources, string resourceName, string culture)
    {
        try
        {
            var fileName = culture == "en-US" 
                ? $"{resourceName}.json" 
                : $"{resourceName}.{GetCultureCode(culture)}.json";
            
            var filePath = Path.Combine(_environment.ContentRootPath, "src", "Shared", "Resources", fileName);
            
            if (File.Exists(filePath))
            {
                var jsonContent = File.ReadAllText(filePath);
                var jsonResources = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                
                if (jsonResources is not null)
                {
                    foreach (var kvp in jsonResources)
                    {
                        resources[kvp.Key] = kvp.Value;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Resource file not found: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON resource '{ResourceName}' for culture '{Culture}'", resourceName, culture);
        }
    }

    private void LoadApiJsonResource(Dictionary<string, string> resources, string resourceName, string culture)
    {
        try
        {
            var fileName = culture == "en-US" 
                ? $"{resourceName}.json" 
                : $"{resourceName}.{GetCultureCode(culture)}.json";
            
            var filePath = Path.Combine(_environment.ContentRootPath, "src", "Api", "Resources", fileName);
            
            if (File.Exists(filePath))
            {
                var jsonContent = File.ReadAllText(filePath);
                var jsonResources = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                
                if (jsonResources is not null)
                {
                    foreach (var kvp in jsonResources)
                    {
                        resources[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading API JSON resource '{ResourceName}' for culture '{Culture}'", resourceName, culture);
        }
    }

    private void LoadAllResources()
    {
        foreach (var culture in _supportedCultures)
        {
            try
            {
                GetResourcesForCulture(culture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-loading resources for culture '{Culture}'", culture);
            }
        }
    }

    private static string GetCultureCode(string culture)
    {
        return culture switch
        {
            "es-ES" => "es",
            "fr-FR" => "fr", 
            "de-DE" => "de",
            "pt-BR" => "pt",
            "it-IT" => "it",
            "ja-JP" => "ja",
            "zh-CN" => "zh",
            "id-ID" => "id",
            _ => culture
        };
    }
}