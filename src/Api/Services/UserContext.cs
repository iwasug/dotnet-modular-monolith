using System.Security.Claims;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Api.Services;

/// <summary>
/// Service for accessing current user context information from HTTP context
/// </summary>
internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public UserId? CurrentUserId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("UserId", out var userIdObj) == true && userIdObj is Guid userId)
            {
                return UserId.From(userId);
            }

            // Fallback to claims if not in context items
            var userIdClaim = context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var claimUserId))
            {
                return UserId.From(claimUserId);
            }

            return null;
        }
    }

    public string? CurrentUserEmail
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("UserEmail", out var emailObj) == true && emailObj is string email)
            {
                return email;
            }

            // Fallback to claims if not in context items
            return context?.User?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    public string? CurrentUserName
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("UserName", out var nameObj) == true && nameObj is string name)
            {
                return name;
            }

            // Fallback to claims if not in context items
            return context?.User?.FindFirst(ClaimTypes.Name)?.Value;
        }
    }

    public IReadOnlyList<Guid> CurrentUserRoleIds
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("UserRoleIds", out var roleIdsObj) == true && roleIdsObj is List<Guid> roleIds)
            {
                return roleIds.AsReadOnly();
            }

            // Fallback to claims if not in context items
            var roleClaims = context?.User?.FindAll("role_id")
                .Select(c => c.Value)
                .Where(v => Guid.TryParse(v, out _))
                .Select(v => Guid.Parse(v))
                .ToList() ?? new List<Guid>();

            return roleClaims.AsReadOnly();
        }
    }

    public string? CurrentTokenId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context?.Items.TryGetValue("TokenId", out var tokenIdObj) == true && tokenIdObj is string tokenId)
            {
                return tokenId;
            }

            // Fallback to claims if not in context items
            return context?.User?.FindFirst("jti")?.Value;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            return context?.User?.Identity?.IsAuthenticated == true;
        }
    }

    public bool HasAnyRole(params Guid[] roleIds)
    {
        if (roleIds is null || roleIds.Length == 0)
        {
            return false;
        }

        var userRoleIds = CurrentUserRoleIds;
        return roleIds.Any(roleId => userRoleIds.Contains(roleId));
    }

    public bool HasAllRoles(params Guid[] roleIds)
    {
        if (roleIds is null || roleIds.Length == 0)
        {
            return true; // Vacuous truth - having all of no roles is true
        }

        var userRoleIds = CurrentUserRoleIds;
        return roleIds.All(roleId => userRoleIds.Contains(roleId));
    }
}