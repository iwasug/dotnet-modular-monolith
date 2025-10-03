using Microsoft.AspNetCore.Authorization;
using ModularMonolith.Api.Authorization.Requirements;
using ModularMonolith.Api.Services;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;

namespace ModularMonolith.Api.Authorization.Handlers;

/// <summary>
/// Authorization handler for permission-based access control
/// </summary>
internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IUserContext _userContext;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IUserContext userContext,
        IRoleRepository roleRepository,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _userContext = userContext;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated || _userContext.CurrentUserId is null)
        {
            _logger.LogDebug("User is not authenticated for permission requirement {Requirement}", requirement);
            context.Fail();
            return;
        }

        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "PermissionAuthorization",
            ["UserId"] = _userContext.CurrentUserId.Value,
            ["RequiredPermission"] = requirement.ToString()
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

            _logger.LogDebug("Checking permission {Permission} for user {UserId} with roles {RoleIds}", 
                requirement, _userContext.CurrentUserId.Value, string.Join(", ", userRoleIds));

            // Check if any of the user's roles have the required permission
            var hasPermission = await CheckUserPermissionsAsync(userRoleIds, requirement);

            if (hasPermission)
            {
                _logger.LogDebug("Permission {Permission} granted for user {UserId}", 
                    requirement, _userContext.CurrentUserId.Value);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogDebug("Permission {Permission} denied for user {UserId}", 
                    requirement, _userContext.CurrentUserId.Value);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", 
                requirement, _userContext.CurrentUserId?.Value);
            context.Fail();
        }
    }

    private async Task<bool> CheckUserPermissionsAsync(
        IReadOnlyList<Guid> userRoleIds, 
        PermissionRequirement requirement)
    {
        // Load all user roles with their permissions
        var userRoles = new List<Role>();
        
        foreach (var roleId in userRoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(RoleId.From(roleId));
            if (role is not null && !role.IsDeleted)
            {
                userRoles.Add(role);
            }
        }

        // Check if any role has the required permission
        foreach (var role in userRoles)
        {
            var permissions = role.GetPermissions();
            
            foreach (var permission in permissions)
            {
                if (requirement.Matches(permission.Resource, permission.Action, permission.Scope))
                {
                    _logger.LogDebug("Permission match found in role {RoleName}: {Permission}", 
                        role.Name.Value, permission);
                    return true;
                }
            }
        }

        return false;
    }
}