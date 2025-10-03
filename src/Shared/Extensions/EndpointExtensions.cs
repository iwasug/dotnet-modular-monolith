using ModularMonolith.Shared.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for endpoint registration
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps all endpoints from modules that implement IEndpointModule
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="assemblies">Assemblies to scan for endpoint modules</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapModuleEndpoints(this WebApplication app, params Assembly[] assemblies)
    {
        var endpointModules = new List<IEndpointModule>();

        foreach (var assembly in assemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpointModule).IsAssignableFrom(t))
                .ToList();

            foreach (var moduleType in moduleTypes)
            {
                if (Activator.CreateInstance(moduleType) is IEndpointModule module)
                {
                    endpointModules.Add(module);
                }
            }
        }

        foreach (var module in endpointModules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }

    /// <summary>
    /// Maps all endpoints from modules that implement IEndpointModule using service provider
    /// </summary>
    /// <param name="app">The web application</param>
    /// <param name="assemblies">Assemblies to scan for endpoint modules</param>
    /// <returns>The web application for chaining</returns>
    public static WebApplication MapModuleEndpointsFromServices(this WebApplication app, params Assembly[] assemblies)
    {
        var serviceProvider = app.Services;
        
        foreach (var assembly in assemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IEndpointModule).IsAssignableFrom(t))
                .ToList();

            foreach (var moduleType in moduleTypes)
            {
                var module = serviceProvider.GetService(moduleType) as IEndpointModule;
                module?.MapEndpoints(app);
            }
        }

        return app;
    }
}