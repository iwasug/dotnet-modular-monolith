namespace ModularMonolith.Shared.Authorization;

/// <summary>
/// Base class for permission constants with common actions and scopes
/// </summary>
public abstract class PermissionConstants
{
    // Common Actions
    public const string READ = "read";
    public const string WRITE = "write";
    public const string DELETE = "delete";
    public const string CREATE = "create";
    public const string UPDATE = "update";
    public const string ASSIGN = "assign";
    public const string REVOKE = "revoke";
    public const string MANAGE = "manage";
    public const string ADMIN = "admin";

    // Common Scopes
    public const string ALL = "*";
    public const string SELF = "self";
    public const string OWN = "own";
    public const string DEPARTMENT = "department";
    public const string ORGANIZATION = "organization";

    // System Resources
    public const string SYSTEM = "*";
}