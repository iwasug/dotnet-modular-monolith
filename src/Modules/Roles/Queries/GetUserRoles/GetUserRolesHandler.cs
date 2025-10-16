using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Services;
using Microsoft.Extensions.Logging;

namespace ModularMonolith.Roles.Queries.GetUserRoles;

/// <summary>
/// Handler for GetUserRolesQuery following the 3-file pattern
/// </summary>
public sealed class GetUserRolesHandler(
    ILogger<GetUserRolesHandler> logger,
    IRoleRepository roleRepository,
    IRoleLocalizationService roleLocalizationService)
    : IQueryHandler<GetUserRolesQuery, GetUserRolesResponse>
{
    private readonly IRoleRepository _roleRepository = roleRepository;

    public Task<Result<GetUserRolesResponse>> Handle(
        GetUserRolesQuery query, 
        CancellationToken cancellationToken = default)
    {
        using var activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Query"] = nameof(GetUserRolesQuery),
            ["UserId"] = query.UserId,
            ["CorrelationId"] = Guid.NewGuid()
        });
        
        logger.LogInformation("Getting roles for user with ID {UserId}", query.UserId);
        
        try
        {
            // TODO: Validate user exists through inter-module communication
            // TODO: Get user's role assignments through inter-module communication
            
            // For now, return empty roles
            logger.LogInformation("User {UserId} has no roles assigned (inter-module communication not implemented)", query.UserId);
            GetUserRolesResponse response = new GetUserRolesResponse(
                query.UserId,
                new List<UserRoleDto>(),
                new List<PermissionDto>()
            );
            
            logger.LogInformation("Retrieved 0 roles for user {UserId}", query.UserId);
            return Task.FromResult(Result<GetUserRolesResponse>.Success(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving roles for user with ID {UserId}", query.UserId);
            return Task.FromResult(Result<GetUserRolesResponse>.Failure(
                Error.Internal("USER_ROLES_RETRIEVAL_FAILED", roleLocalizationService.GetString("UserRolesRetrievalFailed"))));
        }
    }
}