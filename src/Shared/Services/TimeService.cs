using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Default implementation of ITimeService using system time
/// </summary>
public class TimeService : ITimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
    
    public DateTime Now => DateTime.Now;
    
    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
    
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    
    public TimeOnly UtcTimeOfDay => TimeOnly.FromDateTime(DateTime.UtcNow);
    
    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(DateTime.Now);
    
    public DateTimeOffset OffsetNow => DateTimeOffset.Now;
    
    public DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;
}