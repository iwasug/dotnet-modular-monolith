namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Service for password hashing and verification
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a plain text password
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);
}