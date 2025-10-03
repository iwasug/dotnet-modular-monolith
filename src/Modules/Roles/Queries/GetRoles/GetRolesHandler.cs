using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Queries.GetRoles;

/// <summary>
/// Handler for GetRolesQuery following the 3-file pattern
/// </summary>
public sealed class GetRolesHandler : IQueryHandler<GetRolesQuery, GetRolesResponse>
{
    private readonly ILogger<GetRolesHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    
    public GetRolesHandler(
        ILogger<GetRolesHandler> logger,
        IRoleRepository roleRepository)
    {
        _logger = logger;
        _roleRepository = roleRepository;
    }
    
    public async Task<Result<GetRolesResponse>> Handle(
        GetRolesQuery query, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetRolesQuery),
            ["NameFilter"] = query.NameFilter ?? "null",
            ["PageNumber"] = query.PageNumber,
            ["PageSize"] = query.PageSize,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Getting roles with filters - Name: {NameFilter}, Page: {PageNumber}, Size: {PageSize}", 
            query.NameFilter, query.PageNumber, query.PageSize);
        
        try
        {
            // Get all roles (soft delete filter is applied automatically by EF Core global query filter)
            var allRoles = await _roleRepository.GetAllAsync(cancellationToken);

            // Apply filters in memory (in production, this should be done at the database level)
            var filteredRoles = allRoles.AsEnumerable();

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

            var totalCount = filteredRoles.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Apply pagination
            var pagedRoles = filteredRoles
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            // Map to DTOs
            var roleDtos = pagedRoles.Select(role => new RoleDto(
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
            
            _logger.LogInformation("Retrieved {RoleCount} roles out of {TotalCount} total roles", 
                roleDtos.Count, totalCount);
            
            return Result<GetRolesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles with filters");
            return Result<GetRolesResponse>.Failure(
                Error.Internal("ROLES_RETRIEVAL_FAILED", "Failed to retrieve roles"));
        }
    }
}