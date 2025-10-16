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
public sealed class DeleteRoleHandler(
    ILogger<DeleteRoleHandler> logger,
    IRoleRepository roleRepository,
    ITimeService timeService,
    IRoleLocalizationService roleLocalizationService)
    : ICommandHandler<DeleteRoleCommand, DeleteRoleResponse>
{
    public async Task<Result<DeleteRoleResponse>> Handle(
        DeleteRoleCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(DeleteRoleCommand),
            ["RoleId"] = command.Id,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Deleting role with ID {RoleId}", command.Id);
        
        try
        {
            // Check if role exists
            var roleId = RoleId.From(command.Id);
            var roleExists = await roleRepository.ExistsAsync(roleId, cancellationToken);
            
            if (!roleExists)
            {
                logger.LogWarning("Role with ID {RoleId} not found", command.Id);
                return Result<DeleteRoleResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", roleLocalizationService.GetString("RoleNotFound")));
            }

            // Soft delete the role
            await roleRepository.SoftDeleteAsync(roleId, cancellationToken);

            var response = new DeleteRoleResponse(
                command.Id,
                timeService.UtcNow
            );
            
            logger.LogInformation("Role deleted successfully with ID {RoleId}", command.Id);
            
            return Result<DeleteRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting role with ID {RoleId}", command.Id);
            return Result<DeleteRoleResponse>.Failure(
                Error.Internal("ROLE_DELETION_FAILED", roleLocalizationService.GetString("RoleDeletionFailed")));
        }
    }
}
