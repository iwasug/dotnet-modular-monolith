using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain.Services;

/// <summary>
/// Password hashing service using BCrypt
/// </summary>
internal sealed class PasswordHashingService : IPasswordHashingService
{
    private const int WorkFactor = 12; // BCrypt work factor for security vs performance balance

    /// <summary>
    /// Hashes a plain text password using BCrypt with salt rounds
    /// </summary>
    public HashedPassword HashPassword(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(plainTextPassword));
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);
        return HashedPassword.From(hashedPassword);
    }

    /// <summary>
    /// Verifies a plain text password against a hashed password using BCrypt
    /// </summary>
    public bool VerifyPassword(string plainTextPassword, HashedPassword hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        if (hashedPassword is null)
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword.Value);
        }
        catch
        {
            // If verification fails due to invalid hash format or other issues, return false
            return false;
        }
    }
}