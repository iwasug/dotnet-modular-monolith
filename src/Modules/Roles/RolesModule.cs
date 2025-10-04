using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Infrastructure;
using ModularMonolith.Roles.Endpoints;
using ModularMonolith.Roles.Services;
using ModularMonolith.Roles.Commands.CreateRole;
using ModularMonolith.Roles.Commands.UpdateRole;
using ModularMonolith.Roles.Commands.DeleteRole;
using ModularMonolith.Roles.Commands.AssignRoleToUser;
using ModularMonolith.Roles.Queries.GetRole;
using ModularMonolith.Roles.Queries.GetRoles;
using ModularMonolith.Roles.Queries.GetUserRoles;
using FluentValidation;
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

        // Register command handlers
        services.AddScoped<ICommandHandler<CreateRoleCommand, CreateRoleResponse>, CreateRoleHandler>();
        services.AddScoped<ICommandHandler<UpdateRoleCommand, UpdateRoleResponse>, UpdateRoleHandler>();
        services.AddScoped<ICommandHandler<DeleteRoleCommand, DeleteRoleResponse>, DeleteRoleHandler>();
        services.AddScoped<ICommandHandler<AssignRoleToUserCommand, AssignRoleToUserResponse>, AssignRoleToUserHandler>();

        // Register query handlers
        services.AddScoped<IQueryHandler<GetRoleQuery, GetRoleResponse>, GetRoleHandler>();
        services.AddScoped<IQueryHandler<GetRolesQuery, GetRolesResponse>, GetRolesHandler>();
        services.AddScoped<IQueryHandler<GetUserRolesQuery, GetUserRolesResponse>, GetUserRolesHandler>();

        // Register FluentValidation validators
        services.AddScoped<IValidator<CreateRoleCommand>, CreateRoleValidator>();
        services.AddScoped<IValidator<UpdateRoleCommand>, UpdateRoleValidator>();
        services.AddScoped<IValidator<DeleteRoleCommand>, DeleteRoleValidator>();
        services.AddScoped<IValidator<AssignRoleToUserCommand>, AssignRoleToUserValidator>();
        services.AddScoped<IValidator<GetRoleQuery>, GetRoleValidator>();
        services.AddScoped<IValidator<GetRolesQuery>, GetRolesValidator>();
        services.AddScoped<IValidator<GetUserRolesQuery>, GetUserRolesValidator>();
    }

    public void MapEndpoints(WebApplication app)
    {
        RoleEndpoints.MapRoleEndpoints(app);
    }
}