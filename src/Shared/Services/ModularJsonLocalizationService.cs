using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Modular JSON-based implementation of localization service that loads resources from each module
/// </summary>
public sealed class ModularJsonLocalizationService : IModularLocalizationService
{
    private readonly ILogger<ModularJsonLocalizationService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string[] _supportedCultures;
    private readonly Dictionary<string, Dictionary<string, string>> _resourceCache;
    private readonly object _cacheLock = new();

    // Module configuration - defines which modules have localization resources
    private readonly Dictionary<string, string> _moduleResourcePaths = new()
    {
        ["Users"] = "src/Modules/Users/Resources",
        ["Roles"] = "src/Modules/Roles/Resources", 
        ["Authentication"] = "src/Modules/Authentication/Resources",
        ["Api"] = "src/Api/Resources",
        ["Shared"] = "src/Shared/Resources"
    };

    private readonly Dictionary<string, string[]> _moduleResourceFiles = new()
    {
        ["Users"] = new[] { "user-messages" },
        ["Roles"] = new[] { "role-messages" },
        ["Authentication"] = new[] { "auth-messages" },
        ["Api"] = new[] { "api-documentation" },
        ["Shared"] = new[] { "validation-messages", "error-messages" }
    };

    public ModularJsonLocalizationService(ILogger<ModularJsonLocalizationService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _supportedCultures = new[]
        {
            "en-US", "es-ES", "fr-FR", "de-DE",
            "pt-BR", "it-IT", "ja-JP", "zh-CN", "id-ID"
        };
        _resourceCache = new Dictionary<string, Dictionary<string, string>>();
        
        // Pre-load resources from all modules
        LoadAllModuleResources();
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

    /// <summary>
    /// Gets localized string specifically from a module's resources
    /// </summary>
    public string GetModuleString(string moduleName, string key, string? culture = null)
    {
        try
        {
            var cultureInfo = GetCultureInfo(culture);
            var cultureKey = cultureInfo.Name;
            var moduleKey = $"{moduleName}:{cultureKey}";
            
            if (_resourceCache.TryGetValue(moduleKey, out var moduleResources))
            {
                if (moduleResources.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // Fallback to general GetString
            return GetString(key, culture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module string for module '{Module}', key '{Key}', culture '{Culture}'", 
                moduleName, key, culture);
            return key;
        }
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

            // Load resources for this culture from all modules
            var resources = LoadResourcesForCulture(culture);
            _resourceCache[culture] = resources;
            return resources;
        }
    }

    private Dictionary<string, string> LoadResourcesForCulture(string culture)
    {
        var resources = new Dictionary<string, string>();
        
        // Load resources from each module
        foreach (var moduleConfig in _moduleResourcePaths)
        {
            var moduleName = moduleConfig.Key;
            var modulePath = moduleConfig.Value;
            
            if (_moduleResourceFiles.TryGetValue(moduleName, out var resourceFiles))
            {
                foreach (var resourceFile in resourceFiles)
                {
                    LoadModuleJsonResource(resources, modulePath, resourceFile, culture, moduleName);
                }
            }
        }

        return resources;
    }

    private void LoadModuleJsonResource(Dictionary<string, string> resources, string modulePath, 
        string resourceName, string culture, string moduleName)
    {
        try
        {
            var fileName = culture == "en-US" 
                ? $"{resourceName}.json" 
                : $"{resourceName}.{GetCultureCode(culture)}.json";
            
            var filePath = Path.Combine(_environment.ContentRootPath, modulePath, fileName);
            
            if (File.Exists(filePath))
            {
                var jsonContent = File.ReadAllText(filePath);
                var jsonResources = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                
                if (jsonResources is not null)
                {
                    // Store in both general cache and module-specific cache
                    foreach (var kvp in jsonResources)
                    {
                        resources[kvp.Key] = kvp.Value;
                        
                        // Also store with module prefix for module-specific access
                        var moduleKey = $"{moduleName}:{culture}";
                        if (!_resourceCache.ContainsKey(moduleKey))
                        {
                            _resourceCache[moduleKey] = new Dictionary<string, string>();
                        }
                        _resourceCache[moduleKey][kvp.Key] = kvp.Value;
                    }
                    
                    _logger.LogDebug("Loaded {Count} resources from {Module} module for culture {Culture}", 
                        jsonResources.Count, moduleName, culture);
                }
            }
            else
            {
                _logger.LogDebug("Resource file not found: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading module JSON resource '{ResourceName}' from '{ModuleName}' for culture '{Culture}'", 
                resourceName, moduleName, culture);
        }
    }

    private void LoadAllModuleResources()
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
        
        _logger.LogInformation("Pre-loaded localization resources for {CultureCount} cultures from {ModuleCount} modules", 
            _supportedCultures.Length, _moduleResourcePaths.Count);
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