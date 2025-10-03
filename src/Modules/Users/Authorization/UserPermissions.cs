using ModularMonolith.Shared.Domain;
using ModularMonolith.Shared.Authorization;

namespace ModularMonolith.Users.Authorization;

/// <summary>
/// User module permission constants and definitions
/// </summary>
public static class UserPermissions
{
    // Resource name
    public const string RESOURCE = "user";

    // Permission constants
    public static class Actions
    {
        public const string READ = PermissionConstants.READ;
        public const string WRITE = PermissionConstants.WRITE;
        public const string CREATE = PermissionConstants.CREATE;
        public const string UPDATE = PermissionConstants.UPDATE;
        public const string DELETE = PermissionConstants.DELETE;
        public const string MANAGE = PermissionConstants.MANAGE;
    }

    public static class Scopes
    {
        public const string ALL = PermissionConstants.ALL;
        public const string SELF = PermissionConstants.SELF;
        public const string DEPARTMENT = PermissionConstants.DEPARTMENT;
        public const string ORGANIZATION = PermissionConstants.ORGANIZATION;
    }

    // Predefined permissions
    public static readonly Permission ReadAll = Permission.Create(RESOURCE, Actions.READ, Scopes.ALL);
    public static readonly Permission ReadSelf = Permission.Create(RESOURCE, Actions.READ, Scopes.SELF);
    public static readonly Permission ReadDepartment = Permission.Create(RESOURCE, Actions.READ, Scopes.DEPARTMENT);
    public static readonly Permission ReadOrganization = Permission.Create(RESOURCE, Actions.READ, Scopes.ORGANIZATION);

    public static readonly Permission WriteAll = Permission.Create(RESOURCE, Actions.WRITE, Scopes.ALL);
    public static readonly Permission WriteSelf = Permission.Create(RESOURCE, Actions.WRITE, Scopes.SELF);
    public static readonly Permission WriteDepartment = Permission.Create(RESOURCE, Actions.WRITE, Scopes.DEPARTMENT);

    public static readonly Permission CreateAll = Permission.Create(RESOURCE, Actions.CREATE, Scopes.ALL);
    public static readonly Permission CreateDepartment = Permission.Create(RESOURCE, Actions.CREATE, Scopes.DEPARTMENT);

    public static readonly Permission UpdateAll = Permission.Create(RESOURCE, Actions.UPDATE, Scopes.ALL);
    public static readonly Permission UpdateSelf = Permission.Create(RESOURCE, Actions.UPDATE, Scopes.SELF);
    public static readonly Permission UpdateDepartment = Permission.Create(RESOURCE, Actions.UPDATE, Scopes.DEPARTMENT);

    public static readonly Permission DeleteAll = Permission.Create(RESOURCE, Actions.DELETE, Scopes.ALL);
    public static readonly Permission DeleteDepartment = Permission.Create(RESOURCE, Actions.DELETE, Scopes.DEPARTMENT);

    public static readonly Permission ManageAll = Permission.Create(RESOURCE, Actions.MANAGE, Scopes.ALL);
    public static readonly Permission ManageDepartment = Permission.Create(RESOURCE, Actions.MANAGE, Scopes.DEPARTMENT);

    /// <summary>
    /// Gets all user permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAllPermissions()
    {
        return new List<Permission>
        {
            ReadAll, ReadSelf, ReadDepartment, ReadOrganization,
            WriteAll, WriteSelf, WriteDepartment,
            CreateAll, CreateDepartment,
            UpdateAll, UpdateSelf, UpdateDepartment,
            DeleteAll, DeleteDepartment,
            ManageAll, ManageDepartment
        };
    }

    /// <summary>
    /// Gets basic user permissions (commonly used)
    /// </summary>
    public static IReadOnlyList<Permission> GetBasicPermissions()
    {
        return new List<Permission>
        {
            ReadSelf, WriteSelf, UpdateSelf
        };
    }

    /// <summary>
    /// Gets admin user permissions
    /// </summary>
    public static IReadOnlyList<Permission> GetAdminPermissions()
    {
        return new List<Permission>
        {
            ReadAll, WriteAll, CreateAll, UpdateAll, DeleteAll, ManageAll
        };
    }

    /// <summary>
    /// Gets manager user permissions (department level)
    /// </summary>
    public static IReadOnlyList<Permission> GetManagerPermissions()
    {
        return new List<Permission>
        {
            ReadDepartment, WriteDepartment, CreateDepartment, UpdateDepartment, DeleteDepartment, ManageDepartment
        };
    }
}

/// <summary>
/// User module permissions implementation
/// </summary>
internal sealed class UserModulePermissions : IModulePermissions
{
    public string ModuleName => "Users";

    public IReadOnlyList<Permission> GetPermissions()
    {
        return UserPermissions.GetAllPermissions();
    }
}