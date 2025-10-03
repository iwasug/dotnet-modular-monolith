using Microsoft.AspNetCore.Builder;

namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Interface for modules that register endpoints
/// </summary>
public interface IEndpointModule
{
    /// <summary>
    /// Maps the module's endpoints to the application
    /// </summary>
    /// <param name="app">The web application</param>
    void MapEndpoints(WebApplication app);
}