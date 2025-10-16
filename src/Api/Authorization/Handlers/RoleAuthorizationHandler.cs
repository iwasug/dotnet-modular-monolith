using Microsoft.AspNetCore.Authorization;
using ModularMonolith.Api.Authorization.Requirements;
using ModularMonolith.Api.Services;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler for role-based access control
/// </summary>
internal sealed class RoleAuthorizationHandler(
    IUserContext userContext,
    IRoleRepository roleRepository,
    ILogger<RoleAuthorizationHandler> logger)
    : AuthorizationHandler<RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        // Check if user is authenticated
        if (!userContext.IsAuthenticated || userContext.CurrentUserId is null)
        {
            logger.LogDebug("User is not authenticated for role requirement {Requirement}", requirement);
            context.Fail();
            return;
        }

        using IDisposable activity = logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "RoleAuthorization",
            ["UserId"] = userContext.CurrentUserId.Value,
            ["RequiredRoles"] = string.Join(", ", requirement.RequiredRoles),
            ["RequireAllRoles"] = requirement.RequireAllRoles
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

            logger.LogDebug("Checking roles {RequiredRoles} for user {UserId} with roles {UserRoleIds}", 
                string.Join(", ", requirement.RequiredRoles), userContext.CurrentUserId.Value, 
                string.Join(", ", userRoleIds));

            // Get user's role names
            HashSet<string> userRoleNames = await GetUserRoleNamesAsync(userRoleIds);

            // Check role requirements
            bool hasRequiredRoles = requirement.RequireAllRoles
                ? requirement.RequiredRoles.All(requiredRole => userRoleNames.Contains(requiredRole))
                : requirement.RequiredRoles.Any(requiredRole => userRoleNames.Contains(requiredRole));

            if (hasRequiredRoles)
            {
                logger.LogDebug("Role requirement {Requirement} satisfied for user {UserId}", 
                    requirement, userContext.CurrentUserId.Value);
                context.Succeed(requirement);
            }
            else
            {
                logger.LogDebug("Role requirement {Requirement} not satisfied for user {UserId}. User roles: {UserRoles}", 
                    requirement, userContext.CurrentUserId.Value, string.Join(", ", userRoleNames));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking role requirement {Requirement} for user {UserId}", 
                requirement, userContext.CurrentUserId?.Value);
            context.Fail();
        }
    }

    private async Task<HashSet<string>> GetUserRoleNamesAsync(IReadOnlyList<Guid> userRoleIds)
    {
        HashSet<string> roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Guid roleId in userRoleIds)
        {
            Role? role = await roleRepository.GetByIdAsync(RoleId.From(roleId));
            if (role is not null && !role.IsDeleted)
            {
                roleNames.Add(role.Name.Value.ToLowerInvariant());
            }
        }

        return roleNames;
    }
}