using Microsoft.AspNetCore.Authorization;
using ModularMonolith.Api.Authorization.Requirements;
using ModularMonolith.Api.Services;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler for permission-based access control
/// </summary>
internal sealed class PermissionAuthorizationHandler(
    IUserContext userContext,
    IRoleRepository roleRepository,
    ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user is authenticated
        if (!userContext.IsAuthenticated || userContext.CurrentUserId is null)
        {
            logger.LogDebug("User is not authenticated for permission requirement {Requirement}", requirement);
            context.Fail();
            return;
        }

        using IDisposable activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "PermissionAuthorization",
            ["UserId"] = userContext.CurrentUserId.Value,
            ["RequiredPermission"] = requirement.ToString()
        });

        try
        {
            // Get user's role IDs
            IReadOnlyList<Guid> userRoleIds = userContext.CurrentUserRoleIds;
            if (userRoleIds.Count == 0)
            {
                logger.LogDebug("User {UserId} has no roles assigned", userContext.CurrentUserId.Value);
                context.Fail();
                return;
            }

            logger.LogDebug("Checking permission {Permission} for user {UserId} with roles {RoleIds}", 
                requirement, userContext.CurrentUserId.Value, string.Join(", ", userRoleIds));

            // Check if any of the user's roles have the required permission
            bool hasPermission = await CheckUserPermissionsAsync(userRoleIds, requirement);

            if (hasPermission)
            {
                logger.LogDebug("Permission {Permission} granted for user {UserId}", 
                    requirement, userContext.CurrentUserId.Value);
                context.Succeed(requirement);
            }
            else
            {
                logger.LogDebug("Permission {Permission} denied for user {UserId}", 
                    requirement, userContext.CurrentUserId.Value);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", 
                requirement, userContext.CurrentUserId?.Value);
            context.Fail();
        }
    }

    private async Task<bool> CheckUserPermissionsAsync(
        IReadOnlyList<Guid> userRoleIds, 
        PermissionRequirement requirement)
    {
        // Load all user roles with their permissions
        List<Role> userRoles = new List<Role>();
        
        foreach (Guid roleId in userRoleIds)
        {
            Role? role = await roleRepository.GetByIdAsync(RoleId.From(roleId));
            if (role is not null && !role.IsDeleted)
            {
                userRoles.Add(role);
            }
        }

        // Check if any role has the required permission
        foreach (Role role in userRoles)
        {
            IEnumerable<ModularMonolith.Shared.Domain.Permission> permissions = role.GetPermissions();
            
            foreach (ModularMonolith.Shared.Domain.Permission permission in permissions)
            {
                if (requirement.Matches(permission.Resource, permission.Action, permission.Scope))
                {
                    logger.LogDebug("Permission match found in role {RoleName}: {Permission}", 
                        role.Name.Value, permission);
                    return true;
                }
            }
        }

        return false;
    }
}