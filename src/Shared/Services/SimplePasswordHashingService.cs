using System.Security.Cryptography;
using System.Text;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Simple password hashing service for testing purposes
/// In production, use BCrypt or similar
/// </summary>
internal sealed class SimplePasswordHashingService : IPasswordHashingService
{
    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashToVerify = HashPassword(password);
        return hashToVerify == hashedPassword;
    }
}