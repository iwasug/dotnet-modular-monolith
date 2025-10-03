using ModularMonolith.Shared.Domain;
using ModularMonolith.Shared.Authorization;

namespace ModularMonolith.Roles.Authorization;

/// <summary>
/// Role module permission constants and definitions
/// </summary>
public static class RolePermissions
{
    // Resource name
    public const string RESOURCE = "role";

    // Permission constants
    public static class Actions
    {
        public const string READ = PermissionConstants.READ;
        public const string WRITE = PermissionConstants.WRITE;
        public const string CREATE = PermissionConstants.CREATE;
        public const string UPDATE = PermissionConstants.UPDATE;
        public const string DELETE = PermissionConstants.DELETE;
        public const string ASSIGN = PermissionConstants.ASSIGN;
        public const string REVOKE = PermissionConstants.REVOKE;
        public const string MANAGE = PermissionConstants.MANAGE;
    }

    public static class Scopes
    {
        public const string ALL = PermissionConstants.ALL;
        public const string DEPARTMENT = PermissionConstants.DEPARTMENT;
        public const string ORGANIZATION = PermissionConstants.ORGANIZATION;
    }

    // Predefined permissions
    public static readonly Permission ReadAll = Permission.Create(RESOURCE, Actions.READ, Scopes.ALL);
    public static readonly Permission ReadDepartment = Permission.Create(RESOURCE, Actions.READ, Scopes.DEPARTMENT);
    public static readonly Permission ReadOrganization = Permission.Create(RESOURCE, Actions.READ, Scopes.ORGANIZATION);

    public static readonly Permission WriteAll = Permission.Create(RESOURCE, Actions.WRITE, Scopes.ALL);
    public static readonly Permission WriteDepartment = Permission.Create(RESOURCE, Actions.WRITE, Scopes.DEPARTMENT);

    public static readonly Permission CreateAll = Permission.Create(RESOURCE, Actions.CREATE, Scopes.ALL);
    public static readonly Permission CreateDepartment = Permission.Create(RESOURCE, Actions.CREATE, Scopes.DEPARTMENT);

    public static readonly Permission UpdateAll = Permission.Create(RESOURCE, Actions.UPDATE, Scopes.ALL);
    public static readonly Permission UpdateDepartment = Permission.Create(RESOURCE, Actions.UPDATE, Scopes.DEPARTMENT);

    public static readonly Permission DeleteAll = Permission.Create(RESOURCE, Actions.DELETE, Scopes.ALL);
    public static readonly Permission DeleteDepartment = Permission.Create(RESOURCE, Actions.DELETE, Scopes.DEPARTMENT);

    public static readonly Permission AssignAll = Permission.Create(RESOURCE, Actions.ASSIGN, Scopes.ALL);
    public static readonly Permission AssignDepartment = Permission.Create(RESOURCE, Actions.ASSIGN, Scopes.DEPARTMENT);

    public static readonly Permission RevokeAll = Permission.Create(RESOURCE, Actions.REVOKE, Scopes.ALL);
    public static readonly Permission RevokeDepartment = Permission.Create(RESOURCE, Actions.REVOKE, Scopes.DEPARTMENT);

    public static readonly Permission ManageAll = Permission.Create(RESOURCE, Actions.MANAGE, Scopes.ALL);
    public static readonly Permission ManageDepartment = Permission.Create(RESOURCE, Actions.MANAGE, Scopes.DEPARTMENT);

    /// <summary>
    /// Gets all role permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAllPermissions()
    {
        return new List<Permission>
        {
            ReadAll, ReadDepartment, ReadOrganization,
            WriteAll, WriteDepartment,
            CreateAll, CreateDepartment,
            UpdateAll, UpdateDepartment,
            DeleteAll, DeleteDepartment,
            AssignAll, AssignDepartment,
            RevokeAll, RevokeDepartment,
            ManageAll, ManageDepartment
        };
    }

    /// <summary>
    /// Gets basic role permissions (read only)
    /// </summary>
    public static IReadOnlyList<Permission> GetBasicPermissions()
    {
        return new List<Permission>
        {
            ReadOrganization
        };
    }

    /// <summary>
    /// Gets admin role permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAdminPermissions()
    {
        return new List<Permission>
        {
            ReadAll, WriteAll, CreateAll, UpdateAll, DeleteAll, AssignAll, RevokeAll, ManageAll
        };
    }

    /// <summary>
    /// Gets manager role permissions (department level)
    /// </summary>
    public static IReadOnlyList<Permission> GetManagerPermissions()
    {
        return new List<Permission>
        {
            ReadDepartment, WriteDepartment, CreateDepartment, UpdateDepartment, 
            DeleteDepartment, AssignDepartment, RevokeDepartment, ManageDepartment
        };
    }
}

/// <summary>
/// Role module permissions implementation
/// </summary>
internal sealed class RoleModulePermissions : IModulePermissions
{
    public string ModuleName => "Roles";

    public IReadOnlyList<Permission> GetPermissions()
    {
        return RolePermissions.GetAllPermissions();
    }
}