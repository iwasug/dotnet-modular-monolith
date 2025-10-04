using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using ModularMonolith.Roles.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Commands.DeleteRole;

/// <summary>
/// Handler for DeleteRoleCommand with localized error messages
/// </summary>
public sealed class DeleteRoleHandler : ICommandHandler<DeleteRoleCommand, DeleteRoleResponse>
{
    private readonly ILogger<DeleteRoleHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly ITimeService _timeService;
    private readonly IRoleLocalizationService _roleLocalizationService;
    
    public DeleteRoleHandler(
        ILogger<DeleteRoleHandler> logger,
        IRoleRepository roleRepository,
        ITimeService timeService,
        IRoleLocalizationService roleLocalizationService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _timeService = timeService;
        _roleLocalizationService = roleLocalizationService;
    }
    
    public async Task<Result<DeleteRoleResponse>> Handle(
        DeleteRoleCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(DeleteRoleCommand),
            ["RoleId"] = command.Id,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Deleting role with ID {RoleId}", command.Id);
        
        try
        {
            // Check if role exists
            var roleId = RoleId.From(command.Id);
            var roleExists = await _roleRepository.ExistsAsync(roleId, cancellationToken);
            
            if (!roleExists)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", command.Id);
                return Result<DeleteRoleResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", _roleLocalizationService.GetString("RoleNotFound")));
            }

            // Soft delete the role
            await _roleRepository.SoftDeleteAsync(roleId, cancellationToken);

            var response = new DeleteRoleResponse(
                command.Id,
                _timeService.UtcNow
            );
            
            _logger.LogInformation("Role deleted successfully with ID {RoleId}", command.Id);
            
            return Result<DeleteRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with ID {RoleId}", command.Id);
            return Result<DeleteRoleResponse>.Failure(
                Error.Internal("ROLE_DELETION_FAILED", _roleLocalizationService.GetString("RoleDeletionFailed")));
        }
    }
}
