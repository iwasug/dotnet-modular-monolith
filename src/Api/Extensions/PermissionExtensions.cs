using ModularMonolith.Users.Authorization;
using ModularMonolith.Roles.Authorization;
using ModularMonolith.Authentication.Authorization;

namespace ModularMonolith.Api.Extensions;

/// <summary>
/// Extension methods for permission-based authorization using constants
/// </summary>
public static class PermissionConstantExtensions
{
    // User permission extensions using constants
    public static TBuilder RequireUserReadConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.READ);

    public static TBuilder RequireUserWriteConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE);

    public static TBuilder RequireUserCreateConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.CREATE);

    public static TBuilder RequireUserUpdateConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.UPDATE);

    public static TBuilder RequireUserDeleteConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.DELETE);

    public static TBuilder RequireUserManageConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.MANAGE);

    // Role permission extensions using constants
    public static TBuilder RequireRoleReadConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.READ);

    public static TBuilder RequireRoleWriteConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.WRITE);

    public static TBuilder RequireRoleCreateConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.CREATE);

    public static TBuilder RequireRoleUpdateConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.UPDATE);

    public static TBuilder RequireRoleDeleteConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.DELETE);

    public static TBuilder RequireRoleAssignConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.ASSIGN);

    public static TBuilder RequireRoleRevokeConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.REVOKE);

    public static TBuilder RequireRoleManageConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.MANAGE);

    // Authentication permission extensions using constants
    public static TBuilder RequireAuthLoginConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.AUTH_RESOURCE, AuthenticationPermissions.Actions.LOGIN);

    public static TBuilder RequireAuthLogoutConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.AUTH_RESOURCE, AuthenticationPermissions.Actions.LOGOUT);

    public static TBuilder RequireTokenRefreshConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.TOKEN_RESOURCE, AuthenticationPermissions.Actions.REFRESH);

    public static TBuilder RequireTokenRevokeConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.TOKEN_RESOURCE, AuthenticationPermissions.Actions.REVOKE);

    public static TBuilder RequireSessionReadConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.SESSION_RESOURCE, AuthenticationPermissions.Actions.READ);

    public static TBuilder RequireSessionManageConstant<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(AuthenticationPermissions.SESSION_RESOURCE, AuthenticationPermissions.Actions.MANAGE);

    // Scoped permission extensions
    public static TBuilder RequireUserReadSelf<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.READ, UserPermissions.Scopes.SELF);

    public static TBuilder RequireUserWriteSelf<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE, UserPermissions.Scopes.SELF);

    public static TBuilder RequireUserReadDepartment<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.READ, UserPermissions.Scopes.DEPARTMENT);

    public static TBuilder RequireRoleReadDepartment<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.READ, RolePermissions.Scopes.DEPARTMENT);

    public static TBuilder RequireRoleAssignDepartment<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(RolePermissions.RESOURCE, RolePermissions.Actions.ASSIGN, RolePermissions.Scopes.DEPARTMENT);

    // Predefined permission object extensions
    public static TBuilder RequireUserPermission<TBuilder>(this TBuilder builder, ModularMonolith.Shared.Domain.Permission permission) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(permission.Resource, permission.Action, permission.Scope);

    public static TBuilder RequireRolePermission<TBuilder>(this TBuilder builder, ModularMonolith.Shared.Domain.Permission permission) where TBuilder : IEndpointConventionBuilder
        => builder.RequirePermission(permission.Resource, permission.Action, permission.Scope);

    // Convenience methods for predefined permissions
    public static TBuilder RequireUserReadAll<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireUserPermission(UserPermissions.ReadAll);

    public static TBuilder RequireUserWriteAll<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireUserPermission(UserPermissions.WriteAll);

    public static TBuilder RequireRoleReadAll<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireRolePermission(RolePermissions.ReadAll);

    public static TBuilder RequireRoleWriteAll<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireRolePermission(RolePermissions.WriteAll);
}