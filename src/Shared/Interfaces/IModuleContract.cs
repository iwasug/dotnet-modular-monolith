using Microsoft.Extensions.DependencyInjection;

namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Base interface for module communication contracts
/// </summary>
public interface IModuleContract
{
}

/// <summary>
/// Interface for modules to register their services
/// </summary>
public interface IModule
{
    void RegisterServices(IServiceCollection services);
}