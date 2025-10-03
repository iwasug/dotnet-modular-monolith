using ModularMonolith.Api.Middleware;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring comprehensive security middleware
/// </summary>
internal static class SecurityExtensions
{
    /// <summary>
    /// Adds comprehensive security services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddComprehensiveSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure HTTPS redirection
        services.AddHttpsRedirection(options =>
        {
            var httpsSection = configuration.GetSection("Https");
            options.RedirectStatusCode = httpsSection.GetValue<int>("RedirectStatusCode", StatusCodes.Status308PermanentRedirect);
            options.HttpsPort = httpsSection.GetValue<int?>("Port");
        });

        // Configure HSTS (HTTP Strict Transport Security)
        services.AddHsts(options =>
        {
            var hstsSection = configuration.GetSection("Hsts");
            options.MaxAge = TimeSpan.FromDays(hstsSection.GetValue<int>("MaxAgeDays", 365));
            options.IncludeSubDomains = hstsSection.GetValue<bool>("IncludeSubdomains", true);
        });

        // Configure data protection for production scenarios
        services.AddDataProtection();

        // Configure request size limits for security
        services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = configuration.GetValue<long>("Security:MaxRequestBodySize", 10 * 1024 * 1024); // 10MB default
        });

        // Configure Kestrel limits
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            var kestrelSection = configuration.GetSection("Kestrel:Limits");
            options.Limits.MaxRequestBodySize = kestrelSection.GetValue<long>("MaxRequestBodySize", 10 * 1024 * 1024); // 10MB
            options.Limits.MaxRequestHeaderCount = kestrelSection.GetValue<int>("MaxRequestHeaderCount", 100);
            options.Limits.MaxRequestHeadersTotalSize = kestrelSection.GetValue<int>("MaxRequestHeadersTotalSize", 32768); // 32KB
            options.Limits.MaxRequestLineSize = kestrelSection.GetValue<int>("MaxRequestLineSize", 8192); // 8KB
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(kestrelSection.GetValue<int>("RequestHeadersTimeoutSeconds", 30));
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(kestrelSection.GetValue<int>("KeepAliveTimeoutSeconds", 120));
        });

        return services;
    }

    /// <summary>
    /// Configures comprehensive security middleware in the application pipeline
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication UseComprehensiveSecurity(this WebApplication app)
    {
        // Use HSTS in production
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        // Force HTTPS redirection
        app.UseHttpsRedirection();

        // Use security headers middleware
        app.UseMiddleware<SecurityHeadersMiddleware>();

        // Use CORS with secure configuration
        app.UseSecureCors();

        return app;
    }


}