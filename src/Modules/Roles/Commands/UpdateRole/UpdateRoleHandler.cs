using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Commands.UpdateRole;

/// <summary>
/// Handler for UpdateRoleCommand following the 3-file pattern
/// </summary>
public sealed class UpdateRoleHandler : ICommandHandler<UpdateRoleCommand, UpdateRoleResponse>
{
    private readonly ILogger<UpdateRoleHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly ITimeService _timeService;
    
    public UpdateRoleHandler(
        ILogger<UpdateRoleHandler> logger,
        IRoleRepository roleRepository,
        ITimeService timeService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _timeService = timeService;
    }
    
    public async Task<Result<UpdateRoleResponse>> Handle(
        UpdateRoleCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(UpdateRoleCommand),
            ["RoleId"] = command.RoleId,
            ["RoleName"] = command.Name,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Updating role with ID {RoleId}", command.RoleId);
        
        try
        {
            // Get existing role
            var roleId = RoleId.From(command.RoleId);
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", command.RoleId);
                return Result<UpdateRoleResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", "Role not found"));
            }

            // Check if another role with the same name exists (excluding current role)
            var roleName = RoleName.From(command.Name);
            var existingRoleWithName = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (existingRoleWithName is not null && existingRoleWithName.Id != role.Id)
            {
                _logger.LogWarning("Another role with name {RoleName} already exists", command.Name);
                return Result<UpdateRoleResponse>.Failure(
                    Error.Conflict("ROLE_NAME_ALREADY_EXISTS", "Another role with this name already exists"));
            }

            // Update role properties
            role.Update(roleName, command.Description);

            // Update permissions
            if (command.Permissions is not null)
            {
                var permissions = command.Permissions
                    .Select(p => ModularMonolith.Shared.Domain.Permission.Create(p.Resource, p.Action, p.Scope))
                    .ToList();
                
                role.SetPermissions(permissions);
            }

            // Save changes
            await _roleRepository.UpdateAsync(role, cancellationToken);

            var response = new UpdateRoleResponse(
                role.Id,
                role.Name.Value,
                role.Description,
                role.GetPermissions().Select(p => new PermissionDto(p.Resource, p.Action, p.Scope)).ToList(),
                role.UpdatedAt
            );
            
            _logger.LogInformation("Role updated successfully with ID {RoleId}", response.Id);
            
            return Result<UpdateRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID {RoleId}", command.RoleId);
            return Result<UpdateRoleResponse>.Failure(
                Error.Internal("ROLE_UPDATE_FAILED", "Failed to update role"));
        }
    }
}