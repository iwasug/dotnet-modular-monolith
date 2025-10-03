using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ModularMonolith.Shared.Domain;

/// <summary>
/// Base entity configuration that provides common configuration for all entities inheriting from BaseEntity
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        // Configure primary key
        builder.HasKey(e => e.Id);
        
        // Configure Id as UUID v7 with default value generation
        builder.Property(e => e.Id)
            .IsRequired()
            .HasComment("Primary key using UUID v7 for better performance and ordering");

        // Configure audit fields
        ConfigureAuditFields(builder);
        
        // Configure timestamps
        ConfigureTimestamps(builder);
        
        // Configure indexes for performance
        ConfigureIndexes(builder);
        
        // Allow derived classes to add specific configuration
        ConfigureEntity(builder);
    }

    /// <summary>
    /// Configure audit-related fields
    /// </summary>
    protected virtual void ConfigureAuditFields(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Soft delete flag");

        builder.Property(e => e.CreatedBy)
            .IsRequired(false)
            .HasComment("User ID who created this entity");

        builder.Property(e => e.UpdatedBy)
            .IsRequired(false)
            .HasComment("User ID who last updated this entity");

        builder.Property(e => e.DeletedBy)
            .IsRequired(false)
            .HasComment("User ID who soft deleted this entity");

        builder.Property(e => e.DeletedAt)
            .IsRequired(false)
            .HasComment("Timestamp when this entity was soft deleted");
    }

    /// <summary>
    /// Configure timestamp fields
    /// </summary>
    protected virtual void ConfigureTimestamps(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasComment("Timestamp when this entity was created");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasComment("Timestamp when this entity was last updated");
    }

    /// <summary>
    /// Configure indexes for better query performance
    /// </summary>
    protected virtual void ConfigureIndexes(EntityTypeBuilder<TEntity> builder)
    {
        // Index on IsDeleted for soft delete queries
        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsDeleted");

        // Index on CreatedAt for ordering and filtering
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedAt");

        // Index on UpdatedAt for ordering and filtering
        builder.HasIndex(e => e.UpdatedAt)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_UpdatedAt");

        // Composite index for audit queries (non-deleted entities ordered by creation)
        builder.HasIndex(e => new { e.IsDeleted, e.CreatedAt })
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsDeleted_CreatedAt");
    }

    /// <summary>
    /// Override this method in derived classes to add entity-specific configuration
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}