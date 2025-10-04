using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Authentication.Domain;
using ModularMonolith.Authentication.Domain.Services;
using ModularMonolith.Authentication.Infrastructure;
using ModularMonolith.Authentication.Commands.Login;
using ModularMonolith.Authentication.Commands.RefreshToken;
using ModularMonolith.Authentication.Commands.Logout;
using ModularMonolith.Authentication.Commands.Register;
using ModularMonolith.Authentication.Endpoints;
using ModularMonolith.Authentication.Services;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace ModularMonolith.Authentication;

/// <summary>
/// Authentication module registration for dependency injection
/// </summary>
public sealed class AuthenticationModule : IModule, IEndpointModule
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register Feature Management
        services.AddFeatureManagement();

        // Register base repository
        services.AddScoped<RefreshTokenRepository>(provider => 
            new RefreshTokenRepository(
                provider.GetRequiredService<DbContext>(),
                provider.GetRequiredService<ILogger<RefreshTokenRepository>>()));

        // Register cached repository as the main interface implementation
        services.AddScoped<IRefreshTokenRepository>(provider =>
            new CachedRefreshTokenRepository(
                provider.GetRequiredService<RefreshTokenRepository>(),
                provider.GetRequiredService<ICacheService>(),
                provider.GetRequiredService<ILogger<CachedRefreshTokenRepository>>()));

        // Register Authentication module services
        services.AddScoped<ITokenService, Infrastructure.Services.TokenService>();
        services.AddScoped<IAuthenticationService, Infrastructure.Services.AuthenticationService>();

        // Register CQRS handlers
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>, RefreshTokenHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, LogoutResponse>, LogoutHandler>();
        services.AddScoped<ICommandHandler<RegisterCommand, RegisterResponse>, RegisterHandler>();

        // Register localization service
        services.AddScoped<IAuthLocalizationService, AuthLocalizationService>();

        // Register validators
        services.AddScoped<IValidator<LoginCommand>, LoginValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenValidator>();
        services.AddScoped<IValidator<LogoutCommand>, LogoutValidator>();
        services.AddScoped<IValidator<RegisterCommand>, RegisterValidator>();
    }

    public void MapEndpoints(WebApplication app)
    {
        AuthenticationEndpoints.MapAuthenticationEndpoints(app);
    }
}