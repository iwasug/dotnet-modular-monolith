using ModularMonolith.Shared.Domain;
using ModularMonolith.Shared.Interfaces;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain;

/// <summary>
/// User domain entity
/// </summary>
public class User : BaseEntity
{
    public Email Email { get; private set; }
    public HashedPassword Password { get; private set; }
    public UserProfile Profile { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    // Navigation properties
    public List<UserRole> Roles { get; private set; } = new();

    private User() 
    {
        Email = Email.From("temp@example.com");
        Password = HashedPassword.From("temp_hash_placeholder");
        Profile = UserProfile.Create("Temp", "User");
        Roles = new List<UserRole>();
    }

    /// <summary>
    /// Factory method to create a new user using Guid.CreateVersion7()
    /// </summary>
    public static User Create(Email email, HashedPassword password, UserProfile profile)
    {
        return new User
        {
            Email = email,
            Password = password,
            Profile = profile,
            Roles = new List<UserRole>()
        };
    }

    /// <summary>
    /// Factory method to create a new user from string values
    /// </summary>
    public static User Create(string email, string hashedPassword, string firstName, string lastName)
    {
        return Create(
            Email.From(email),
            HashedPassword.From(hashedPassword),
            UserProfile.Create(firstName, lastName)
        );
    }

    /// <summary>
    /// Updates the last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = GetCurrentTime();
        UpdateTimestamp();
    }



    /// <summary>
    /// Updates the user's profile information
    /// </summary>
    public void UpdateProfile(UserProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        UpdateTimestamp();
    }

    /// <summary>
    /// Changes the user's password
    /// </summary>
    public void ChangePassword(HashedPassword newPassword)
    {
        Password = newPassword ?? throw new ArgumentNullException(nameof(newPassword));
        UpdateTimestamp();
    }

    /// <summary>
    /// Assigns a role to the user
    /// </summary>
    public void AssignRole(ModularMonolith.Roles.Domain.ValueObjects.RoleId roleId, UserId? assignedBy = null)
    {
        if (roleId is null)
        {
            throw new ArgumentNullException(nameof(roleId));
        }

        // Check if user already has this role
        if (Roles.Any(ur => ur.RoleId == roleId))
        {
            return; // Role already assigned
        }

        var userRole = UserRole.Create(UserId.From(Id), roleId, assignedBy);
        Roles.Add(userRole);
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes a role from the user
    /// </summary>
    public void RemoveRole(ModularMonolith.Roles.Domain.ValueObjects.RoleId roleId)
    {
        if (roleId is null)
        {
            throw new ArgumentNullException(nameof(roleId));
        }

        var userRole = Roles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole is not null)
        {
            Roles.Remove(userRole);
            UpdateTimestamp();
        }
    }

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    public bool HasRole(ModularMonolith.Roles.Domain.ValueObjects.RoleId roleId)
    {
        if (roleId is null) return false;
        return Roles.Any(ur => ur.RoleId == roleId);
    }

    /// <summary>
    /// Gets all role IDs assigned to the user
    /// </summary>
    public IReadOnlyList<ModularMonolith.Roles.Domain.ValueObjects.RoleId> GetRoleIds()
    {
        return Roles.Select(ur => ur.RoleId).ToList().AsReadOnly();
    }

    private static DateTime GetCurrentTime()
    {
        // Use the same time service pattern as BaseEntity
        return DateTime.UtcNow;
    }
}