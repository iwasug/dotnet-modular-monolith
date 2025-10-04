using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Shared.Services;
using ModularMonolith.Shared.Authorization;
using ModularMonolith.Shared.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularMonolith.Shared.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to support modular architecture
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared kernel services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // Register time service
        services.AddSingleton<ITimeService, TimeService>();
        
        // Register password hashing service
        services.AddSingleton<IPasswordHashingService, SimplePasswordHashingService>();
        
        // Register permission registry
        services.AddSingleton<Authorization.PermissionRegistry>();
        
        // Register modular JSON-based localization service
        services.AddSingleton<IModularLocalizationService, ModularJsonLocalizationService>();
        services.AddSingleton<ILocalizationService>(provider => provider.GetRequiredService<IModularLocalizationService>());
        
        // Register localized error service
        services.AddSingleton<ILocalizedErrorService, LocalizedErrorService>();
        
        // Register email service
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddTransient<IEmailService, EmailService>();
        
        // Register other shared services here
        return services;
    }

    /// <summary>
    /// Registers a module with the dependency injection container
    /// </summary>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services)
        where TModule : class, IModule, new()
    {
        var module = new TModule();
        module.RegisterServices(services);
        return services;
    }
    
    /// <summary>
    /// Adds fake time service for testing
    /// </summary>
    public static IServiceCollection AddFakeTimeService(this IServiceCollection services, DateTime? fixedTime = null)
    {
        services.AddSingleton<ITimeService>(provider => new FakeTimeService(fixedTime));
        return services;
    }
}