using ModularMonolith.Shared.Domain;

namespace ModularMonolith.Shared.Authorization;

/// <summary>
/// Interface for module permission definitions
/// </summary>
public interface IModulePermissions
{
    /// <summary>
    /// Gets all permissions defined in this module
    /// </summary>
    /// <returns>List of permissions</returns>
    IReadOnlyList<Permission> GetPermissions();

    /// <summary>
    /// Gets the module name
    /// </summary>
    string ModuleName { get; }
}