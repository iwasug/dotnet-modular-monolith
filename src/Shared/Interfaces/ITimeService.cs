namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Time service interface for consistent time handling across the application
/// </summary>
public interface ITimeService
{
    /// <summary>
    /// Gets the current UTC date and time
    /// </summary>
    DateTime UtcNow { get; }
    
    /// <summary>
    /// Gets the current local date and time
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// Gets the current date only (UTC)
    /// </summary>
    DateOnly UtcToday { get; }
    
    /// <summary>
    /// Gets the current date only (local)
    /// </summary>
    DateOnly Today { get; }
    
    /// <summary>
    /// Gets the current time only (UTC)
    /// </summary>
    TimeOnly UtcTimeOfDay { get; }
    
    /// <summary>
    /// Gets the current time only (local)
    /// </summary>
    TimeOnly TimeOfDay { get; }
    
    /// <summary>
    /// Gets the current DateTimeOffset
    /// </summary>
    DateTimeOffset OffsetNow { get; }
    
    /// <summary>
    /// Gets the current UTC DateTimeOffset
    /// </summary>
    DateTimeOffset OffsetUtcNow { get; }
}