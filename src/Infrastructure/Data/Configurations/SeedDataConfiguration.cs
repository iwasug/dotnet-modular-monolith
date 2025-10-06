using Microsoft.EntityFrameworkCore;
using ModularMonolith.Users.Domain;
using ModularMonolith.Roles.Domain;
using System.Security.Cryptography;
using System.Text;

namespace ModularMonolith.Infrastructure.Data.Configurations;

/// <summary>
/// Configuration for seeding initial data
/// </summary>
public static class SeedDataConfiguration
{
    // Predefined IDs for seed data (using fixed GUIDs for consistency)
    private static readonly Guid AdminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid RegularUserId = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid AdminUserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000020");
    private static readonly Guid RegularUserRoleId = Guid.Parse("00000000-0000-0000-0000-000000000021");

    private static readonly DateTime SeedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void SeedData(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedUsers(modelBuilder);
        SeedUserRoles(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasData(
                new
                {
                    Id = AdminRoleId,
                    Name = "Admin", // Will be converted by EF Core's value converter
                    Description = "Administrator role with full system access",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                },
                new
                {
                    Id = UserRoleId,
                    Name = "User",
                    Description = "Standard user role with basic access",
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                }
            );
        });
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        // Hash the default passwords
        var adminPassword = HashPassword("Admin@123");
        var userPassword = HashPassword("User@123");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasData(
                new
                {
                    Id = AdminUserId,
                    Email = "admin@example.com", // Will be converted by EF Core's value converter
                    Password = adminPassword, // Will be converted by EF Core's value converter
                    LastLoginAt = (DateTime?)null,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                },
                new
                {
                    Id = RegularUserId,
                    Email = "user@example.com",
                    Password = userPassword,
                    LastLoginAt = (DateTime?)null,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                }
            );

            // Seed owned entity (Profile) data separately
            entity.OwnsOne(u => u.Profile).HasData(
                new
                {
                    UserId = AdminUserId,
                    FirstName = "Admin",
                    LastName = "User"
                },
                new
                {
                    UserId = RegularUserId,
                    FirstName = "Regular",
                    LastName = "User"
                }
            );
        });
    }

    private static void SeedUserRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasData(
                new
                {
                    Id = AdminUserRoleId,
                    UserId = AdminUserId, // Will be converted by EF Core's value converter
                    RoleId = AdminRoleId, // Will be converted by EF Core's value converter
                    AssignedBy = (Guid?)null,
                    AssignedAt = SeedDate,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                },
                new
                {
                    Id = RegularUserRoleId,
                    UserId = RegularUserId,
                    RoleId = UserRoleId,
                    AssignedBy = (Guid?)null,
                    AssignedAt = SeedDate,
                    CreatedAt = SeedDate,
                    UpdatedAt = SeedDate,
                    CreatedBy = (Guid?)null,
                    UpdatedBy = (Guid?)null,
                    DeletedBy = (Guid?)null,
                    DeletedAt = (DateTime?)null,
                    IsDeleted = false
                }
            );
        });
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}
