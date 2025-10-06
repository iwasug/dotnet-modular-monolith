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
internal sealed class GetRoleHandler : IQueryHandler<GetRoleQuery, GetRoleResponse>
{
    private readonly ILogger<GetRoleHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleLocalizationService _roleLocalizationService;
    
    public GetRoleHandler(
        ILogger<GetRoleHandler> logger,
        IRoleRepository roleRepository,
        IRoleLocalizationService roleLocalizationService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _roleLocalizationService = roleLocalizationService;
    }
    
    public async Task<Result<GetRoleResponse>> Handle(
        GetRoleQuery query, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetRoleQuery),
            ["RoleId"] = query.RoleId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Getting role with ID {RoleId}", query.RoleId);
        
        try
        {
            var roleId = RoleId.From(query.RoleId);
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            
            if (role is null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", query.RoleId);
                return Result<GetRoleResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", _roleLocalizationService.GetString("RoleNotFound")));
            }

            var response = new GetRoleResponse(
                role.Id,
                role.Name.Value,
                role.Description,
                role.GetPermissions().Select(p => new PermissionDto(p.Resource, p.Action, p.Scope)).ToList(),
                role.CreatedAt,
                role.UpdatedAt
            );
            
            _logger.LogInformation("Role retrieved successfully with ID {RoleId}", query.RoleId);
            return Result<GetRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID {RoleId}", query.RoleId);
            return Result<GetRoleResponse>.Failure(
                Error.Internal("ROLE_RETRIEVAL_FAILED", _roleLocalizationService.GetString("RoleNotFound")));
        }
    }
}