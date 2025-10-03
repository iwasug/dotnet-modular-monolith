using ModularMonolith.Shared.Common;
using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain.Services;

/// <summary>
/// Interface for user validation business rules
/// </summary>
public interface IUserValidationService
{
    /// <summary>
    /// Validates that an email is not already in use
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <param name="excludeUserId">Optional user ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if the email is available</returns>
    Task<Result> ValidateEmailAvailabilityAsync(Email email, UserId? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates password strength requirements
    /// </summary>
    /// <param name="plainTextPassword">The plain text password to validate</param>
    /// <returns>Result indicating if the password meets requirements</returns>
    Result ValidatePasswordStrength(string plainTextPassword);

    /// <summary>
    /// Validates user profile information
    /// </summary>
    /// <param name="profile">The user profile to validate</param>
    /// <returns>Result indicating if the profile is valid</returns>
    Result ValidateUserProfile(UserProfile profile);

    /// <summary>
    /// Validates that a user can be deactivated
    /// </summary>
    /// <param name="user">The user to validate</param>
    /// <returns>Result indicating if the user can be deactivated</returns>
    Result ValidateUserDeactivation(User user);
}