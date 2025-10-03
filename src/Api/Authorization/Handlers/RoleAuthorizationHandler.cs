using Microsoft.AspNetCore.Authorization;
using ModularMonolith.Api.Authorization.Requirements;
using ModularMonolith.Api.Services;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler for role-based access control
/// </summary>
internal sealed class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IUserContext _userContext;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RoleAuthorizationHandler> _logger;

    public RoleAuthorizationHandler(
        IUserContext userContext,
        IRoleRepository roleRepository,
        ILogger<RoleAuthorizationHandler> logger)
    {
        _userContext = userContext;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated || _userContext.CurrentUserId is null)
        {
            _logger.LogDebug("User is not authenticated for role requirement {Requirement}", requirement);
            context.Fail();
            return;
        }

        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "RoleAuthorization",
            ["UserId"] = _userContext.CurrentUserId.Value,
            ["RequiredRoles"] = string.Join(", ", requirement.RequiredRoles),
            ["RequireAllRoles"] = requirement.RequireAllRoles
        });

        try
        {
            // Get user's role IDs
            var userRoleIds = _userContext.CurrentUserRoleIds;
            if (userRoleIds.Count == 0)
            {
                _logger.LogDebug("User {UserId} has no roles assigned", _userContext.CurrentUserId.Value);
                context.Fail();
                return;
            }

            _logger.LogDebug("Checking roles {RequiredRoles} for user {UserId} with roles {UserRoleIds}", 
                string.Join(", ", requirement.RequiredRoles), _userContext.CurrentUserId.Value, 
                string.Join(", ", userRoleIds));

            // Get user's role names
            var userRoleNames = await GetUserRoleNamesAsync(userRoleIds);

            // Check role requirements
            var hasRequiredRoles = requirement.RequireAllRoles
                ? requirement.RequiredRoles.All(requiredRole => userRoleNames.Contains(requiredRole))
                : requirement.RequiredRoles.Any(requiredRole => userRoleNames.Contains(requiredRole));

            if (hasRequiredRoles)
            {
                _logger.LogDebug("Role requirement {Requirement} satisfied for user {UserId}", 
                    requirement, _userContext.CurrentUserId.Value);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogDebug("Role requirement {Requirement} not satisfied for user {UserId}. User roles: {UserRoles}", 
                    requirement, _userContext.CurrentUserId.Value, string.Join(", ", userRoleNames));
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking role requirement {Requirement} for user {UserId}", 
                requirement, _userContext.CurrentUserId?.Value);
            context.Fail();
        }
    }

    private async Task<HashSet<string>> GetUserRoleNamesAsync(IReadOnlyList<Guid> userRoleIds)
    {
        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleId in userRoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(RoleId.From(roleId));
            if (role is not null && !role.IsDeleted)
            {
                roleNames.Add(role.Name.Value.ToLowerInvariant());
            }
        }

        return roleNames;
    }
}