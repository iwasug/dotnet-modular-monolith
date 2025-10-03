using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Shared.Domain;

/// <summary>
/// Base entity class with UUID v7 ID generation for better performance and ordering
/// </summary>
public abstract class BaseEntity
{
    private static ITimeService? _timeService;
    
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public Guid? CreatedBy { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }
    public Guid? DeletedBy { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }

    protected BaseEntity()
    {
        var now = GetCurrentTime();
        CreatedAt = now;
        UpdatedAt = now;
        IsDeleted = false;
    }

    public void UpdateTimestamp(Guid? updatedBy = null)
    {
        UpdatedAt = GetCurrentTime();
        UpdatedBy = updatedBy;
    }

    public void SetCreatedBy(Guid createdBy)
    {
        CreatedBy = createdBy;
    }

    public void SoftDelete(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = GetCurrentTime();
        // UpdateTimestamp will be called automatically by DbContext
    }

    public void Restore(Guid? restoredBy = null)
    {
        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
        // UpdateTimestamp will be called automatically by DbContext
    }
    
    /// <summary>
    /// Sets the time service for all entities (used for testing)
    /// </summary>
    public static void SetTimeService(ITimeService timeService)
    {
        _timeService = timeService;
    }
    
    /// <summary>
    /// Clears the time service (reverts to DateTime.UtcNow)
    /// </summary>
    public static void ClearTimeService()
    {
        _timeService = null;
    }
    
    private static DateTime GetCurrentTime()
    {
        return _timeService?.Now ?? DateTime.UtcNow;
    }
}