using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace ModularMonolith.Infrastructure.Configuration;

/// <summary>
/// Service for validating application configuration at startup
/// </summary>
public sealed class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(IConfiguration configuration, ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates all critical configuration settings
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var errors = new List<string>();
        
        try
        {
            _logger.LogInformation("Starting configuration validation");

            // Validate database configuration
            ValidateDatabase(errors);
            
            // Validate cache configuration
            ValidateCache(errors);
            
            // Validate JWT configuration
            ValidateJwt(errors);
            
            // Validate logging configuration
            ValidateLogging(errors);
            
            // Validate CORS configuration
            ValidateCors(errors);

            if (errors.Count == 0)
            {
                _logger.LogInformation("Configuration validation completed successfully");
                return ValidationResult.Success!;
            }
            else
            {
                var errorMessage = $"Configuration validation failed with {errors.Count} errors: {string.Join("; ", errors)}";
                _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", errors));
                return new ValidationResult(errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during configuration validation");
            return new ValidationResult($"Configuration validation failed with exception: {ex.Message}");
        }
    }

    private void ValidateDatabase(List<string> errors)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("Database connection string 'DefaultConnection' is missing or empty");
            return;
        }

        // Validate connection string format for PostgreSQL
        if (!connectionString.Contains("Host=") && !connectionString.Contains("Server="))
        {
            errors.Add("Database connection string must contain Host or Server parameter");
        }

        if (!connectionString.Contains("Database="))
        {
            errors.Add("Database connection string must contain Database parameter");
        }

        // Check for security best practices
        if (connectionString.Contains("Password=") && connectionString.Contains("password123"))
        {
            errors.Add("Database connection string contains default/weak password");
        }

        _logger.LogDebug("Database configuration validation completed");
    }

    private void ValidateCache(List<string> errors)
    {
        var cacheProvider = _configuration["Cache:Provider"];
        
        if (string.IsNullOrWhiteSpace(cacheProvider))
        {
            errors.Add("Cache provider is not configured");
            return;
        }

        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
        {
            var redisConnectionString = _configuration["Cache:Redis:ConnectionString"];
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                errors.Add("Redis cache provider is selected but connection string is missing");
            }
            
            var keyPrefix = _configuration["Cache:Redis:KeyPrefix"];
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                errors.Add("Redis key prefix is missing - this could cause key conflicts");
            }
        }

        // Validate cache expiration settings
        var defaultExpirationString = _configuration["Cache:DefaultExpiration"];
        if (!string.IsNullOrWhiteSpace(defaultExpirationString))
        {
            if (!TimeSpan.TryParse(defaultExpirationString, out var expiration) || expiration <= TimeSpan.Zero)
            {
                errors.Add("Cache default expiration is invalid or zero/negative");
            }
        }

        _logger.LogDebug("Cache configuration validation completed");
    }

    private void ValidateJwt(List<string> errors)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            errors.Add("JWT signing key is missing");
        }
        else if (jwtKey.Length < 32)
        {
            errors.Add("JWT signing key is too short (minimum 32 characters recommended)");
        }
        else if (jwtKey == "your-super-secret-key-here" || jwtKey == "default-key")
        {
            errors.Add("JWT signing key appears to be a default/example value");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            errors.Add("JWT issuer is missing");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            errors.Add("JWT audience is missing");
        }

        // Validate token expiration settings
        var accessTokenExpirationString = _configuration["Jwt:AccessTokenExpiration"];
        if (!string.IsNullOrWhiteSpace(accessTokenExpirationString))
        {
            if (!TimeSpan.TryParse(accessTokenExpirationString, out var expiration) || expiration <= TimeSpan.Zero)
            {
                errors.Add("JWT access token expiration is invalid or zero/negative");
            }
            else if (expiration > TimeSpan.FromHours(24))
            {
                errors.Add("JWT access token expiration is too long (maximum 24 hours recommended)");
            }
        }

        _logger.LogDebug("JWT configuration validation completed");
    }

    private void ValidateLogging(List<string> errors)
    {
        var loggingSection = _configuration.GetSection("Serilog");
        
        if (!loggingSection.Exists())
        {
            errors.Add("Serilog logging configuration is missing");
            return;
        }

        // Check for minimum log level configuration
        var minLevel = _configuration["Serilog:MinimumLevel:Default"];
        if (string.IsNullOrWhiteSpace(minLevel))
        {
            errors.Add("Serilog minimum log level is not configured");
        }

        // Validate write-to sinks
        var writeTo = _configuration.GetSection("Serilog:WriteTo");
        if (!writeTo.Exists() || !writeTo.GetChildren().Any())
        {
            errors.Add("No Serilog write-to sinks are configured");
        }

        _logger.LogDebug("Logging configuration validation completed");
    }

    private void ValidateCors(List<string> errors)
    {
        var corsSection = _configuration.GetSection("Cors");
        
        if (!corsSection.Exists())
        {
            // CORS configuration is optional, but log a warning
            _logger.LogWarning("CORS configuration is not present - this may cause issues in browser-based applications");
            return;
        }

        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is not null && allowedOrigins.Contains("*"))
        {
            errors.Add("CORS is configured to allow all origins (*) - this is a security risk in production");
        }

        _logger.LogDebug("CORS configuration validation completed");
    }

    /// <summary>
    /// Gets a summary of the current configuration for diagnostic purposes
    /// </summary>
    public ConfigurationSummary GetConfigurationSummary()
    {
        return new ConfigurationSummary
        {
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
            DatabaseProvider = "PostgreSQL",
            CacheProvider = _configuration["Cache:Provider"] ?? "InMemory",
            JwtConfigured = !string.IsNullOrWhiteSpace(_configuration["Jwt:Key"]),
            LoggingConfigured = _configuration.GetSection("Serilog").Exists(),
            CorsConfigured = _configuration.GetSection("Cors").Exists(),
            HealthChecksEnabled = _configuration.GetValue<bool>("HealthChecks:Enabled", true),
            SwaggerEnabled = _configuration.GetValue<bool>("Swagger:Enabled", true)
        };
    }
}

/// <summary>
/// Summary of application configuration
/// </summary>
public sealed class ConfigurationSummary
{
    public string Environment { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = string.Empty;
    public string CacheProvider { get; set; } = string.Empty;
    public bool JwtConfigured { get; set; }
    public bool LoggingConfigured { get; set; }
    public bool CorsConfigured { get; set; }
    public bool HealthChecksEnabled { get; set; }
    public bool SwaggerEnabled { get; set; }
}