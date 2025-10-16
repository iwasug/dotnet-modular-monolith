using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery following the 3-file pattern
/// </summary>
public sealed class GetRolesHandler(
    ILogger<GetRolesHandler> logger,
    IRoleRepository roleRepository,
    IRoleLocalizationService roleLocalizationService)
    : IQueryHandler<GetRolesQuery, GetRolesResponse>
{
    public async Task<Result<GetRolesResponse>> Handle(
        GetRolesQuery query, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetRolesQuery),
            ["NameFilter"] = query.NameFilter ?? "null",
            ["PageNumber"] = query.PageNumber,
            ["PageSize"] = query.PageSize,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Getting roles with filters - Name: {NameFilter}, Page: {PageNumber}, Size: {PageSize}", 
            query.NameFilter, query.PageNumber, query.PageSize);
        
        try
        {
            // Get all roles (soft delete filter is applied automatically by EF Core global query filter)
            IReadOnlyList<Role> allRoles = await roleRepository.GetAllAsync(cancellationToken);

            // Apply filters in memory (in production, this should be done at the database level)
            IEnumerable<Role> filteredRoles = allRoles.AsEnumerable();

            // Apply name filter
            if (!string.IsNullOrWhiteSpace(query.NameFilter))
            {
                filteredRoles = filteredRoles.Where(r => 
                    r.Name.Value.Contains(query.NameFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply permission filters
            if (!string.IsNullOrWhiteSpace(query.PermissionResource))
            {
                filteredRoles = filteredRoles.Where(r => 
                    r.GetPermissions().Any(p => 
                        p.Resource.Contains(query.PermissionResource, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(query.PermissionAction))
            {
                filteredRoles = filteredRoles.Where(r => 
                    r.GetPermissions().Any(p => 
                        p.Action.Contains(query.PermissionAction, StringComparison.OrdinalIgnoreCase)));
            }

            int totalCount = filteredRoles.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Apply pagination
            List<Role> pagedRoles = filteredRoles
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            // Map to DTOs
            List<RoleDto> roleDtos = pagedRoles.Select(role => new RoleDto(
                role.Id,
                role.Name.Value,
                role.Description,
                role.GetPermissions().Select(p => new PermissionDto(p.Resource, p.Action, p.Scope)).ToList(),
                role.CreatedAt,
                role.UpdatedAt
            )).ToList();

            var response = new GetRolesResponse(
                roleDtos,
                totalCount,
                query.PageNumber,
                query.PageSize,
                totalPages
            );
            
            logger.LogInformation("Retrieved {RoleCount} roles out of {TotalCount} total roles", 
                roleDtos.Count, totalCount);
            
            return Result<GetRolesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving roles with filters");
            return Result<GetRolesResponse>.Failure(
                Error.Internal("ROLES_RETRIEVAL_FAILED", roleLocalizationService.GetString("RolesRetrievalFailed")));
        }
    }
}