using ModularMonolith.Api.Middleware;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for configuring global exception handling
/// </summary>
internal static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the application pipeline
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>The web application builder for chaining</returns>
    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }
}