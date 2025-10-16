using System.Security.Cryptography;

namespace ModularMonolith.Api.Middleware;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
{
    private readonly string _nonce = GenerateNonce();

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        await next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        var headers = response.Headers;

        try
        {
            // X-Content-Type-Options: Prevent MIME type sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // X-Frame-Options: Prevent clickjacking
            headers.Append("X-Frame-Options", "DENY");

            // X-XSS-Protection: Enable XSS filtering
            headers.Append("X-XSS-Protection", "1; mode=block");

            // Referrer-Policy: Control referrer information
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Content-Security-Policy: Prevent XSS and other injection attacks
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self'; " +
                     "connect-src 'self'; " +
                     "frame-ancestors 'none'; " +
                     "base-uri 'self'; " +
                     "form-action 'self'";
            headers.Append("Content-Security-Policy", csp);

            // Strict-Transport-Security: Enforce HTTPS (only add if HTTPS)
            if (context.Request.IsHttps)
            {
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            // Permissions-Policy: Control browser features
            headers.Append("Permissions-Policy", 
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), magnetometer=(), gyroscope=()");

            // X-Permitted-Cross-Domain-Policies: Restrict cross-domain policies
            headers.Append("X-Permitted-Cross-Domain-Policies", "none");

            // Cache-Control for API responses (prevent caching of sensitive data)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
                headers.Append("Pragma", "no-cache");
                headers.Append("Expires", "0");
            }

            logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add security headers for {Path}", context.Request.Path);
        }
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>
/// Extension methods for SecurityHeadersMiddleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}