using ModularMonolith.Users.Domain.ValueObjects;

namespace ModularMonolith.Users.Domain.Services;

/// <summary>
/// Interface for password hashing operations
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// </summary>
    /// <param name="plainTextPassword">The plain text password to hash</param>
    /// <returns>A hashed password value object</returns>
    HashedPassword HashPassword(string plainTextPassword);

    /// <summary>
    /// Verifies a plain text password against a hashed password
    /// </summary>
    /// <param name="plainTextPassword">The plain text password to verify</param>
    /// <param name="hashedPassword">The hashed password to verify against</param>
    /// <returns>True if the password matches, false otherwise</returns>
    bool VerifyPassword(string plainTextPassword, HashedPassword hashedPassword);
}