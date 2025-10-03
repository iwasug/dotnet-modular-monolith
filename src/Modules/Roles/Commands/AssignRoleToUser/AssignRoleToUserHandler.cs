using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Commands.AssignRoleToUser;

/// <summary>
/// Handler for AssignRoleToUserCommand following the 3-file pattern
/// </summary>
public sealed class AssignRoleToUserHandler : ICommandHandler<AssignRoleToUserCommand, AssignRoleToUserResponse>
{
    private readonly ILogger<AssignRoleToUserHandler> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly ITimeService _timeService;
    
    public AssignRoleToUserHandler(
        ILogger<AssignRoleToUserHandler> logger,
        IRoleRepository roleRepository,
        ITimeService timeService)
    {
        _logger = logger;
        _roleRepository = roleRepository;
        _timeService = timeService;
    }
    
    public async Task<Result<AssignRoleToUserResponse>> Handle(
        AssignRoleToUserCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(AssignRoleToUserCommand),
            ["UserId"] = command.UserId,
            ["RoleId"] = command.RoleId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        _logger.LogInformation("Assigning role {RoleId} to user {UserId}", command.RoleId, command.UserId);
        
        try
        {
            // Validate role exists
            var roleId = RoleId.From(command.RoleId);
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
            {
                _logger.LogWarning("Role with ID {RoleId} not found", command.RoleId);
                return Result<AssignRoleToUserResponse>.Failure(
                    Error.NotFound("ROLE_NOT_FOUND", "Role not found"));
            }

            // TODO: Validate user exists through inter-module communication
            // TODO: Check if user already has this role
            // TODO: Validate assignedBy user if provided
            // TODO: Assign role to user through inter-module communication

            var response = new AssignRoleToUserResponse(
                command.UserId,
                command.RoleId,
                role.Name.Value,
                command.AssignedBy,
                _timeService.UtcNow
            );
            
            _logger.LogInformation("Role {RoleId} assigned successfully to user {UserId}", command.RoleId, command.UserId);
            
            return Result<AssignRoleToUserResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", command.RoleId, command.UserId);
            return Result<AssignRoleToUserResponse>.Failure(
                Error.Internal("ROLE_ASSIGNMENT_FAILED", "Failed to assign role to user"));
        }
    }
}