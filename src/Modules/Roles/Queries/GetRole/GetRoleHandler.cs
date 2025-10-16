using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using ModularMonolith.Roles.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Queries.GetRole;

/// <summary>
/// Handler for GetRoleQuery following the 3-file pattern
/// </summary>
internal sealed class GetRoleHandler(
    ILogger<GetRoleHandler> logger,
    IRoleRepository roleRepository,
    IRoleLocalizationService roleLocalizationService)
    : IQueryHandler<GetRoleQuery, GetRoleResponse>
{
    public async Task<Result<GetRoleResponse>> Handle(
        GetRoleQuery query, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetRoleQuery),
            ["RoleId"] = query.RoleId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Getting role with ID {RoleId}", query.RoleId);
        
        try
        {
            var roleId = RoleId.From(query.RoleId);
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            
            if (role is null)
            {
                logger.LogWarning("Role with ID {RoleId} not found", query.RoleId);
                return Result<GetRoleResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", roleLocalizationService.GetString("RoleNotFound")));
            }

            var response = new GetRoleResponse(
                role.Id,
                role.Name.Value,
                role.Description,
                role.GetPermissions().Select(p => new PermissionDto(p.Resource, p.Action, p.Scope)).ToList(),
                role.CreatedAt,
                role.UpdatedAt
            );
            
            logger.LogInformation("Role retrieved successfully with ID {RoleId}", query.RoleId);
            return Result<GetRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving role with ID {RoleId}", query.RoleId);
            return Result<GetRoleResponse>.Failure(
                Error.Internal("ROLE_RETRIEVAL_FAILED", roleLocalizationService.GetString("RoleNotFound")));
        }
    }
}