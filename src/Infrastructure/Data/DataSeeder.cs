using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolith.Users.Domain;
using ModularMonolith.Roles.Domain;
using ModularMonolith.Roles.Domain.ValueObjects;
using System.Security.Cryptography;
using System.Text;

namespace ModularMonolith.Infrastructure.Data;

/// <summary>
/// Seeds initial data into the database
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    // Predefined IDs for seed data
    private static readonly Guid AdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid RegularUserId = Guid.Parse("00000000-0000-0000-0000-000000000011");

    public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds initial data if database is empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Check if data already exists
            if (await _context.Roles.AnyAsync() || await _context.Users.AnyAsync())
            {
                _logger.LogInformation("Database already contains data. Skipping seed.");
                return;
            }

            _logger.LogInformation("Seeding initial data...");

            await SeedRolesAsync();
            await SeedUsersAsync();
            
            _logger.LogInformation("Initial data seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding data.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var adminRole = Role.Create("Admin", "Administrator role with full system access");
        SetEntityId(adminRole, AdminRoleId);

        var userRole = Role.Create("User", "Standard user role with basic access");
        SetEntityId(userRole, UserRoleId);

        await _context.Roles.AddRangeAsync(adminRole, userRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Roles seeded: Admin, User");
    }

    private async Task SeedUsersAsync()
    {
        var adminPassword = HashPassword("Admin@123");
        var userPassword = HashPassword("User@123");

        var adminUser = User.Create("admin@example.com", adminPassword, "Admin", "User");
        SetEntityId(adminUser, AdminUserId);
        adminUser.AssignRole(RoleId.From(AdminRoleId));

        var regularUser = User.Create("user@example.com", userPassword, "Regular", "User");
        SetEntityId(regularUser, RegularUserId);
        regularUser.AssignRole(RoleId.From(UserRoleId));

        await _context.Users.AddRangeAsync(adminUser, regularUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Users seeded: admin@example.com (Admin), user@example.com (User)");
    }

    private static void SetEntityId<TEntity>(TEntity entity, Guid id) where TEntity : class
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(entity, id);
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}
