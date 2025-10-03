using ModularMonolith.Shared.Domain;
using ModularMonolith.Shared.Authorization;

namespace ModularMonolith.Authentication.Authorization;

/// <summary>
/// Authentication module permission constants and definitions
/// </summary>
public static class AuthenticationPermissions
{
    // Resource names
    public const string AUTH_RESOURCE = "auth";
    public const string TOKEN_RESOURCE = "token";
    public const string SESSION_RESOURCE = "session";

    // Permission constants
    public static class Actions
    {
        public const string LOGIN = "login";
        public const string LOGOUT = "logout";
        public const string REFRESH = "refresh";
        public const string REVOKE = PermissionConstants.REVOKE;
        public const string READ = PermissionConstants.READ;
        public const string MANAGE = PermissionConstants.MANAGE;
        public const string ADMIN = PermissionConstants.ADMIN;
    }

    public static class Scopes
    {
        public const string ALL = PermissionConstants.ALL;
        public const string SELF = PermissionConstants.SELF;
        public const string DEPARTMENT = PermissionConstants.DEPARTMENT;
        public const string ORGANIZATION = PermissionConstants.ORGANIZATION;
    }

    // Authentication permissions
    public static readonly Permission LoginAll = Permission.Create(AUTH_RESOURCE, Actions.LOGIN, Scopes.ALL);
    public static readonly Permission LogoutAll = Permission.Create(AUTH_RESOURCE, Actions.LOGOUT, Scopes.ALL);
    public static readonly Permission LogoutSelf = Permission.Create(AUTH_RESOURCE, Actions.LOGOUT, Scopes.SELF);

    // Token permissions
    public static readonly Permission RefreshTokenSelf = Permission.Create(TOKEN_RESOURCE, Actions.REFRESH, Scopes.SELF);
    public static readonly Permission RevokeTokenAll = Permission.Create(TOKEN_RESOURCE, Actions.REVOKE, Scopes.ALL);
    public static readonly Permission RevokeTokenSelf = Permission.Create(TOKEN_RESOURCE, Actions.REVOKE, Scopes.SELF);
    public static readonly Permission RevokeTokenDepartment = Permission.Create(TOKEN_RESOURCE, Actions.REVOKE, Scopes.DEPARTMENT);

    // Session permissions
    public static readonly Permission ReadSessionAll = Permission.Create(SESSION_RESOURCE, Actions.READ, Scopes.ALL);
    public static readonly Permission ReadSessionSelf = Permission.Create(SESSION_RESOURCE, Actions.READ, Scopes.SELF);
    public static readonly Permission ReadSessionDepartment = Permission.Create(SESSION_RESOURCE, Actions.READ, Scopes.DEPARTMENT);

    public static readonly Permission ManageSessionAll = Permission.Create(SESSION_RESOURCE, Actions.MANAGE, Scopes.ALL);
    public static readonly Permission ManageSessionDepartment = Permission.Create(SESSION_RESOURCE, Actions.MANAGE, Scopes.DEPARTMENT);

    // Admin permissions
    public static readonly Permission AuthAdmin = Permission.Create(AUTH_RESOURCE, Actions.ADMIN, Scopes.ALL);
    public static readonly Permission TokenAdmin = Permission.Create(TOKEN_RESOURCE, Actions.ADMIN, Scopes.ALL);
    public static readonly Permission SessionAdmin = Permission.Create(SESSION_RESOURCE, Actions.ADMIN, Scopes.ALL);

    /// <summary>
    /// Gets all authentication permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAllPermissions()
    {
        return new List<Permission>
        {
            LoginAll, LogoutAll, LogoutSelf,
            RefreshTokenSelf, RevokeTokenAll, RevokeTokenSelf, RevokeTokenDepartment,
            ReadSessionAll, ReadSessionSelf, ReadSessionDepartment,
            ManageSessionAll, ManageSessionDepartment,
            AuthAdmin, TokenAdmin, SessionAdmin
        };
    }

    /// <summary>
    /// Gets basic authentication permissions (for regular users)
    /// </summary>
    public static IReadOnlyList<Permission> GetBasicPermissions()
    {
        return new List<Permission>
        {
            LoginAll, LogoutSelf, RefreshTokenSelf, ReadSessionSelf
        };
    }

    /// <summary>
    /// Gets admin authentication permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAdminPermissions()
    {
        return new List<Permission>
        {
            AuthAdmin, TokenAdmin, SessionAdmin,
            RevokeTokenAll, ReadSessionAll, ManageSessionAll
        };
    }

    /// <summary>
    /// Gets manager authentication permissions (department level)
    /// </summary>
    public static IReadOnlyList<Permission> GetManagerPermissions()
    {
        return new List<Permission>
        {
            RevokeTokenDepartment, ReadSessionDepartment, ManageSessionDepartment
        };
    }
}

/// <summary>
/// Authentication module permissions implementation
/// </summary>
internal sealed class AuthenticationModulePermissions : IModulePermissions
{
    public string ModuleName => "Authentication";

    public IReadOnlyList<Permission> GetPermissions()
    {
        return AuthenticationPermissions.GetAllPermissions();
    }
}