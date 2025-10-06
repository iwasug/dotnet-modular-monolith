using ModularMonolith.Shared.Authorization;
using ModularMonolith.Shared.Extensions;
using ModularMonolith.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace ModularMonolith.Api.Endpoints;

/// <summary>
/// Permission discovery and management endpoints
/// </summary>
internal static class PermissionEndpoints
{
    /// <summary>
    /// Maps permission endpoints to the application
    /// </summary>
    internal static void MapPermissionEndpoints(this WebApplication app)
    {
        var permissions = app.MapGroup("/api/permissions")
            .WithTags("Permissions");

        // GET /api/permissions - Get all permissions
        permissions.MapGet("/", (
            [FromServices] PermissionRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var allPermissions = registry.GetAllPermissions();
            
            var data = new
            {
                TotalCount = allPermissions.Count,
                Permissions = allPermissions.Select(p => new
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    FullName = $"{p.Resource}:{p.Action}:{p.Scope}"
                }).ToList()
            };

            return Results.Ok(ApiResponse<object>.Ok(data, "All permissions retrieved successfully"));
        })
        .WithName("GetAllPermissions")
        .WithSummary("Get all registered permissions")
        .WithDescription("Retrieves all permissions from all modules")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(401)
        .Produces(403);

        // GET /api/permissions/modules - Get permissions by module
        permissions.MapGet("/modules", (
            [FromServices] PermissionRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var permissionsByModule = registry.GetPermissionsByModule();
            
            var data = permissionsByModule.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    ModuleName = kvp.Key,
                    Count = kvp.Value.Count,
                    Permissions = kvp.Value.Select(p => new
                    {
                        Resource = p.Resource,
                        Action = p.Action,
                        Scope = p.Scope,
                        FullName = $"{p.Resource}:{p.Action}:{p.Scope}"
                    }).ToList()
                }
            );

            return Results.Ok(ApiResponse<object>.Ok(data, "Permissions by module retrieved successfully"));
        })
        .WithName("GetPermissionsByModule")
        .WithSummary("Get permissions grouped by module")
        .WithDescription("Retrieves permissions organized by their source module")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(401)
        .Produces(403);

        // GET /api/permissions/resources - Get permissions by resource
        permissions.MapGet("/resources", (
            [FromServices] PermissionRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var permissionsByResource = registry.GetPermissionsByResource();
            
            var data = permissionsByResource.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Resource = kvp.Key,
                    Count = kvp.Value.Count,
                    Permissions = kvp.Value.Select(p => new
                    {
                        Action = p.Action,
                        Scope = p.Scope,
                        FullName = $"{p.Resource}:{p.Action}:{p.Scope}"
                    }).ToList()
                }
            );

            return Results.Ok(ApiResponse<object>.Ok(data, "Permissions by resource retrieved successfully"));
        })
        .WithName("GetPermissionsByResource")
        .WithSummary("Get permissions grouped by resource")
        .WithDescription("Retrieves permissions organized by their target resource")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(401)
        .Produces(403);

        // GET /api/permissions/statistics - Get permission statistics
        permissions.MapGet("/statistics", (
            [FromServices] PermissionRegistry registry,
            CancellationToken cancellationToken) =>
        {
            var statistics = registry.GetStatistics();
            
            var data = new
            {
                statistics.TotalPermissions,
                statistics.TotalModules,
                statistics.TotalResources,
                ModuleBreakdown = statistics.ModulePermissionCounts,
                ResourceBreakdown = statistics.ResourcePermissionCounts,
                Summary = statistics.ToString()
            };

            return Results.Ok(ApiResponse<object>.Ok(data, "Permission statistics retrieved successfully"));
        })
        .WithName("GetPermissionStatistics")
        .WithSummary("Get permission statistics")
        .WithDescription("Retrieves statistical information about registered permissions")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(401)
        .Produces(403);

        // GET /api/permissions/search - Search permissions
        permissions.MapGet("/search", (
            [FromServices] PermissionRegistry registry,
            [FromQuery] string? resource = null,
            [FromQuery] string? action = null,
            [FromQuery] string? scope = null,
            CancellationToken cancellationToken = default) =>
        {
            var allPermissions = registry.GetAllPermissions();
            var filteredPermissions = allPermissions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(resource))
            {
                filteredPermissions = filteredPermissions.Where(p => 
                    p.Resource.Contains(resource, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                filteredPermissions = filteredPermissions.Where(p => 
                    p.Action.Contains(action, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(scope))
            {
                filteredPermissions = filteredPermissions.Where(p => 
                    p.Scope.Contains(scope, StringComparison.OrdinalIgnoreCase));
            }

            var results = filteredPermissions.ToList();
            
            var data = new
            {
                SearchCriteria = new { resource, action, scope },
                TotalResults = results.Count,
                Permissions = results.Select(p => new
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope,
                    FullName = $"{p.Resource}:{p.Action}:{p.Scope}"
                }).ToList()
            };

            return Results.Ok(ApiResponse<object>.Ok(data, "Permission search completed successfully"));
        })
        .WithName("SearchPermissions")
        .WithSummary("Search permissions")
        .WithDescription("Search permissions by resource, action, or scope")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(401)
        .Produces(403);

        // GET /api/permissions/{resource}/{action} - Find specific permission
        permissions.MapGet("/{resource}/{action}", (
            [FromRoute] string resource,
            [FromRoute] string action,
            [FromServices] PermissionRegistry registry,
            [FromQuery] string scope = "*",
            CancellationToken cancellationToken = default) =>
        {
            var permission = registry.FindPermission(resource, action, scope);
            
            if (permission is null)
            {
                var error = Error.NotFound("PERMISSION_NOT_FOUND", $"Permission not found: {resource}:{action}:{scope}");
                return Results.NotFound(ApiResponse<object>.Fail(error.Message, error));
            }

            var data = new
            {
                Found = true,
                Permission = new
                {
                    Resource = permission.Resource,
                    Action = permission.Action,
                    Scope = permission.Scope,
                    FullName = $"{permission.Resource}:{permission.Action}:{permission.Scope}"
                }
            };

            return Results.Ok(ApiResponse<object>.Ok(data, "Permission found successfully"));
        })
        .WithName("FindPermission")
        .WithSummary("Find specific permission")
        .WithDescription("Find a specific permission by resource, action, and scope")
        .RequireSystemAdmin()
        .Produces(200)
        .Produces(404)
        .Produces(401)
        .Produces(403);
    }
}