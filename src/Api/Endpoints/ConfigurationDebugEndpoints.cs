using Microsoft.AspNetCore.Mvc;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Endpoints for debugging configuration (Development only)
/// </summary>
public class ConfigurationDebugEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/debug/config")
            .WithTags("Configuration Debug")
            .WithOpenApi();

#if DEBUG
        group.MapGet("/environment", GetEnvironmentInfo)
            .WithName("GetEnvironmentInfo")
            .WithSummary("Get current environment information")
            .Produces<object>();

        group.MapGet("/serilog", GetSerilogConfig)
            .WithName("GetSerilogConfig")
            .WithSummary("Get current Serilog configuration")
            .Produces<object>();

        group.MapGet("/connection-strings", GetConnectionStrings)
            .WithName("GetConnectionStrings")
            .WithSummary("Get current connection strings")
            .Produces<object>();

        group.MapGet("/all-config", GetAllConfiguration)
            .WithName("GetAllConfiguration")
            .WithSummary("Get all configuration values")
            .Produces<object>();
#endif
    }

#if DEBUG
    private static IResult GetEnvironmentInfo(
        [FromServices] IWebHostEnvironment environment)
    {
        return Results.Ok(new
        {
            EnvironmentName = environment.EnvironmentName,
            IsDevelopment = environment.IsDevelopment(),
            IsProduction = environment.IsProduction(),
            IsStaging = environment.IsStaging(),
            ContentRootPath = environment.ContentRootPath,
            WebRootPath = environment.WebRootPath,
            ApplicationName = environment.ApplicationName,
            EnvironmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }

    private static IResult GetSerilogConfig(
        [FromServices] IConfiguration configuration)
    {
        var serilogSection = configuration.GetSection("Serilog");
        
        return Results.Ok(new
        {
            MinimumLevel = serilogSection.GetSection("MinimumLevel").Get<object>(),
            WriteTo = serilogSection.GetSection("WriteTo").Get<object[]>(),
            Properties = serilogSection.GetSection("Properties").Get<object>(),
            Enrich = serilogSection.GetSection("Enrich").Get<string[]>(),
            Override = serilogSection.GetSection("MinimumLevel:Override").Get<Dictionary<string, string>>()
        });
    }

    private static IResult GetConnectionStrings(
        [FromServices] IConfiguration configuration)
    {
        var connectionStrings = configuration.GetSection("ConnectionStrings").Get<Dictionary<string, string>>();
        
        return Results.Ok(new
        {
            ConnectionStrings = connectionStrings
        });
    }

    private static IResult GetAllConfiguration(
        [FromServices] IConfiguration configuration)
    {
        var allConfig = new Dictionary<string, object>();
        
        foreach (var section in configuration.GetChildren())
        {
            allConfig[section.Key] = GetSectionValue(section);
        }

        return Results.Ok(allConfig);
    }

    private static object GetSectionValue(IConfigurationSection section)
    {
        if (section.GetChildren().Any())
        {
            var dict = new Dictionary<string, object>();
            foreach (var child in section.GetChildren())
            {
                dict[child.Key] = GetSectionValue(child);
            }
            return dict;
        }
        
        return section.Value ?? "";
    }
#endif
}