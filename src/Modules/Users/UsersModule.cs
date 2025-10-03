using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Commands.CreateUser;
using ModularMonolith.Users.Queries.GetUser;
using ModularMonolith.Users.Infrastructure;
using ModularMonolith.Users.Domain;
using ModularMonolith.Users.Domain.Services;
using ModularMonolith.Users.Endpoints;
using ModularMonolith.Users.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace ModularMonolith.Users;

/// <summary>
/// Users module registration for dependency injection
/// </summary>
public sealed class UsersModule : IModule, IEndpointModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register domain services
        services.AddScoped<ModularMonolith.Users.Domain.Services.IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IUserValidationService, UserValidationService>();

        // Register base repository
        services.AddScoped<UserRepository>(provider => 
            new UserRepository(
                provider.GetRequiredService<DbContext>(),
                provider.GetRequiredService<ILogger<UserRepository>>()));

        // Register cached repository as the main interface implementation
        services.AddScoped<IUserRepository>(provider =>
            new CachedUserRepository(
                provider.GetRequiredService<UserRepository>(),
                provider.GetRequiredService<ICacheService>(),
                provider.GetRequiredService<ILogger<CachedUserRepository>>()));

        // Register command handlers
        services.AddScoped<ICommandHandler<CreateUserCommand, CreateUserResponse>, CreateUserHandler>();

        // Register query handlers
        services.AddScoped<IQueryHandler<GetUserQuery, GetUserResponse>, GetUserHandler>();

        // Register FluentValidation validators
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserValidator>();
        services.AddScoped<IValidator<GetUserQuery>, GetUserValidator>();
        
        // Configure validator lifetime scopes - validators are typically scoped
        // This ensures proper dependency injection for validators that depend on other services
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>(ServiceLifetime.Scoped);
        
        // Register localization service
        services.AddScoped<IUserLocalizationService, UserLocalizationService>();
    }

    public void MapEndpoints(WebApplication app)
    {
        UserEndpoints.MapUserEndpoints(app);
    }
}