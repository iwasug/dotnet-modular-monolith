using Microsoft.AspNetCore.Mvc;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Services;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Endpoints for entity discovery and debugging (Development only)
/// </summary>
public class EntityDiscoveryEndpoints : IEndpointModule
{
    public void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/dev/entities")
            .WithTags("Entity Discovery")
            .WithOpenApi();

#if DEBUG
        group.MapGet("/stats", GetEntityStats)
            .WithName("GetEntityStats")
            .WithSummary("Get entity registration statistics")
            .Produces<object>();

        group.MapGet("/details", GetEntityDetails)
            .WithName("GetEntityDetails")
            .WithSummary("Get detailed information about all registered entities")
            .Produces<IEnumerable<object>>();

        group.MapGet("/validate", ValidateEntityRegistration)
            .WithName("ValidateEntityRegistration")
            .WithSummary("Validate that all discovered entities are properly registered")
            .Produces<object>();

        group.MapGet("/discovered", GetDiscoveredEntities)
            .WithName("GetDiscoveredEntities")
            .WithSummary("Get all discovered entity types")
            .Produces<IEnumerable<object>>();
#endif
    }

#if DEBUG
    private static IResult GetEntityStats(
        [FromServices] ApplicationDbContext context,
        [FromServices] IEntityDiscoveryService entityDiscoveryService)
    {
        var stats = entityDiscoveryService.GetEntityStats(context);
        
        return Results.Ok(new
        {
            stats.TotalEntities,
            stats.BaseEntities,
            ModuleBreakdown = stats.ModuleStats,
            Summary = stats.ToString()
        });
    }

    private static IResult GetEntityDetails(
        [FromServices] ApplicationDbContext context,
        [FromServices] IEntityDiscoveryService entityDiscoveryService)
    {
        var details = entityDiscoveryService.GetEntityDetails(context);
        
        var result = details.Select(d => new
        {
            d.Name,
            d.FullName,
            d.Namespace,
            d.Module,
            d.Assembly,
            d.IsBaseEntity
        }).ToList();

        return Results.Ok(result);
    }

    private static IResult ValidateEntityRegistration(
        [FromServices] ApplicationDbContext context,
        [FromServices] IEntityDiscoveryService entityDiscoveryService)
    {
        var validation = entityDiscoveryService.ValidateEntityRegistration(context);
        
        return Results.Ok(new
        {
            validation.IsValid,
            validation.DiscoveredCount,
            validation.RegisteredCount,
            MissingTypes = validation.MissingTypes.Select(t => new
            {
                Name = t.Name,
                FullName = t.FullName,
                Namespace = t.Namespace
            }).ToList(),
            ExtraTypes = validation.ExtraTypes.Select(t => new
            {
                Name = t.Name,
                FullName = t.FullName,
                Namespace = t.Namespace
            }).ToList(),
            Summary = validation.ToString()
        });
    }

    private static IResult GetDiscoveredEntities(
        [FromServices] IEntityDiscoveryService entityDiscoveryService)
    {
        var discoveredTypes = entityDiscoveryService.DiscoverEntityTypes();
        
        var result = discoveredTypes.Select(t => new
        {
            Name = t.Name,
            FullName = t.FullName,
            Namespace = t.Namespace,
            Assembly = t.Assembly.GetName().Name
        }).ToList();

        return Results.Ok(result);
    }
#endif
}