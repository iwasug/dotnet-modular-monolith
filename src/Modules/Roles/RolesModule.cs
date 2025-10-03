using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Infrastructure;
using ModularMonolith.Roles.Endpoints;
using ModularMonolith.Roles.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ModularMonolith.Roles;

/// <summary>
/// Roles module registration for dependency injection
/// </summary>
public sealed class RolesModule : IModule, IEndpointModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register base repository
        services.AddScoped<RoleRepository>(provider => 
            new RoleRepository(
                provider.GetRequiredService<DbContext>(),
                provider.GetRequiredService<ILogger<RoleRepository>>()));

        // Register cached repository as the main interface implementation
        services.AddScoped<IRoleRepository>(provider =>
            new CachedRoleRepository(
                provider.GetRequiredService<RoleRepository>(),
                provider.GetRequiredService<ICacheService>(),
                provider.GetRequiredService<ILogger<CachedRoleRepository>>()));

        // Register localization service
        services.AddScoped<IRoleLocalizationService, RoleLocalizationService>();
        
        // Register Roles module services
        // Command and query handlers will be registered here
    }

    public void MapEndpoints(WebApplication app)
    {
        RoleEndpoints.MapRoleEndpoints(app);
    }
}