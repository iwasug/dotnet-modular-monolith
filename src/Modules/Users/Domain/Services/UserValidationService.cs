using ModularMonolith.Shared.Common;
using ModularMonolith.Users.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace ModularMonolith.Users.Domain.Services;

/// <summary>
/// User validation service implementing business rules
/// </summary>
public class UserValidationService : IUserValidationService
{
    private readonly IUserRepository _userRepository;
    
    // Password strength requirements
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        RegexOptions.Compiled);

    public UserValidationService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Validates that an email is not already in use
    /// </summary>
    public async Task<Result> ValidateEmailAvailabilityAsync(Email email, UserId? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        
        if (existingUser == null)
        {
            return Result.Success();
        }

        // If we're excluding a specific user (for updates), check if it's the same user
        if (excludeUserId != null && existingUser.Id == excludeUserId.Value)
        {
            return Result.Success();
        }

        return Result.Failure(Error.Conflict("EMAIL_ALREADY_EXISTS", $"Email '{email}' is already in use"));
    }

    /// <summary>
    /// Validates password strength requirements
    /// </summary>
    public Result ValidatePasswordStrength(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return Result.Failure(Error.Validation("PASSWORD_REQUIRED", "Password is required"));
        }

        if (plainTextPassword.Length < 8)
        {
            return Result.Failure(Error.Validation("PASSWORD_TOO_SHORT", "Password must be at least 8 characters long"));
        }

        if (plainTextPassword.Length > 128)
        {
            return Result.Failure(Error.Validation("PASSWORD_TOO_LONG", "Password must not exceed 128 characters"));
        }

        if (!PasswordRegex.IsMatch(plainTextPassword))
        {
            return Result.Failure(Error.Validation("PASSWORD_WEAK", 
                "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates user profile information
    /// </summary>
    public Result ValidateUserProfile(UserProfile profile)
    {
        if (profile == null)
        {
            return Result.Failure(Error.Validation("PROFILE_REQUIRED", "User profile is required"));
        }

        // Additional business rules can be added here
        // For example: checking for inappropriate content, reserved names, etc.
        
        var forbiddenNames = new[] { "admin", "administrator", "system", "root", "test" };
        
        if (forbiddenNames.Contains(profile.FirstName.ToLowerInvariant()) || 
            forbiddenNames.Contains(profile.LastName.ToLowerInvariant()))
        {
            return Result.Failure(Error.Validation("FORBIDDEN_NAME", "The provided name contains forbidden words"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates that a user can be deactivated
    /// </summary>
    public Result ValidateUserDeactivation(User user)
    {
        if (user == null)
        {
            return Result.Failure(Error.Validation("USER_REQUIRED", "User is required"));
        }

        if (user.IsDeleted)
        {
            return Result.Failure(Error.Validation("USER_ALREADY_DELETED", "User is already deleted"));
        }

        // Additional business rules for deactivation
        // For example: prevent deactivation of the last admin user
        // TODO: Implement role checking with UserRoles navigation property when needed

        return Result.Success();
    }
}