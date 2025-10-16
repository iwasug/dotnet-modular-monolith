using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using ModularMonolith.Roles.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Commands.CreateRole;

/// <summary>
/// Handler for CreateRoleCommand following the 3-file pattern
/// </summary>
public sealed class CreateRoleHandler(
    ILogger<CreateRoleHandler> logger,
    IRoleRepository roleRepository,
    ITimeService timeService,
    IRoleLocalizationService roleLocalizationService)
    : ICommandHandler<CreateRoleCommand, CreateRoleResponse>
{
    private readonly ITimeService _timeService = timeService;

    public async Task<Result<CreateRoleResponse>> Handle(
        CreateRoleCommand command, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = nameof(CreateRoleCommand),
            ["RoleName"] = command.Name,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Creating role with name {RoleName}", command.Name);
        
        try
        {
            // Check if role already exists
            RoleName roleName = RoleName.From(command.Name);
            Role? existingRole = await roleRepository.GetByNameAsync(roleName, cancellationToken);
            if (existingRole is not null)
            {
                logger.LogWarning("Role with name {RoleName} already exists", command.Name);
                return Result<CreateRoleResponse>.Failure(
                    Error.Conflict("ROLE_ALREADY_EXISTS", roleLocalizationService.GetString("RoleAlreadyExists")));
            }

            // Create role entity
            var role = Role.Create(roleName, command.Description);

            // Add permissions if provided
            if (command.Permissions is not null && command.Permissions.Count > 0)
            {
                List<ModularMonolith.Shared.Domain.Permission> permissions = command.Permissions
                    .Select(p => ModularMonolith.Shared.Domain.Permission.Create(p.Resource, p.Action, p.Scope))
                    .ToList();
                
                role.SetPermissions(permissions);
            }

            // Save to repository
            await roleRepository.AddAsync(role, cancellationToken);

            var response = new CreateRoleResponse(
                role.Id,
                role.Name.Value,
                role.Description,
                role.GetPermissions().Select(p => new PermissionDto(p.Resource, p.Action, p.Scope)).ToList(),
                role.CreatedAt
            );
            
            logger.LogInformation("Role created successfully with ID {RoleId}", response.Id);
            
            return Result<CreateRoleResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role with name {RoleName}", command.Name);
            return Result<CreateRoleResponse>.Failure(
                Error.Internal("ROLE_CREATION_FAILED", roleLocalizationService.GetString("RoleCreationFailed")));
        }
    }
}