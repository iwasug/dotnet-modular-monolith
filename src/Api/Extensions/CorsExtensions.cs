namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for CORS configuration
/// </summary>
public static class CorsExtensions
{
    private const string DefaultPolicyName = "DefaultCorsPolicy";

    /// <summary>
    /// Adds CORS services with secure default configuration
    /// </summary>
    public static IServiceCollection AddSecureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("Cors");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "Content-Type", "Authorization", "Accept", "X-Requested-With" };
        var allowCredentials = corsSettings.GetValue<bool>("AllowCredentials", false);
        var maxAge = corsSettings.GetValue<int>("MaxAge", 86400); // 24 hours

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0 && !allowedOrigins.Contains("*"))
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    // For development only - in production, specify exact origins
                    policy.AllowAnyOrigin();
                }

                policy.WithMethods(allowedMethods)
                      .WithHeaders(allowedHeaders)
                      .SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge));

                // Only allow credentials if not using AllowAnyOrigin
                if (allowCredentials && allowedOrigins.Length > 0 && !allowedOrigins.Contains("*"))
                {
                    policy.AllowCredentials();
                }

                // Add security headers for CORS preflight requests
                policy.WithExposedHeaders("X-Pagination-Count", "X-Pagination-Page", "X-Pagination-Limit");
            });

            // Add a restrictive policy for sensitive endpoints
            options.AddPolicy("RestrictivePolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins.Where(o => o != "*").ToArray())
                      .WithMethods("GET", "POST")
                      .WithHeaders("Content-Type", "Authorization")
                      .SetPreflightMaxAge(TimeSpan.FromSeconds(300)); // 5 minutes for sensitive endpoints
            });
        });

        return services;
    }

    /// <summary>
    /// Uses CORS with the default policy
    /// </summary>
    public static IApplicationBuilder UseSecureCors(this IApplicationBuilder app)
    {
        return app.UseCors(DefaultPolicyName);
    }
}