using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Fake time service for testing purposes
/// </summary>
internal sealed class FakeTimeService : ITimeService
{
    private DateTime _currentTime;
    
    public FakeTimeService(DateTime? fixedTime = null)
    {
        _currentTime = fixedTime ?? new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }
    
    public DateTime UtcNow => _currentTime.Kind == DateTimeKind.Utc 
        ? _currentTime 
        : _currentTime.ToUniversalTime();
    
    public DateTime Now => _currentTime.Kind == DateTimeKind.Local 
        ? _currentTime 
        : _currentTime.ToLocalTime();
    
    public DateOnly UtcToday => DateOnly.FromDateTime(UtcNow);
    
    public DateOnly Today => DateOnly.FromDateTime(Now);
    
    public TimeOnly UtcTimeOfDay => TimeOnly.FromDateTime(UtcNow);
    
    public TimeOnly TimeOfDay => TimeOnly.FromDateTime(Now);
    
    public DateTimeOffset OffsetNow => new DateTimeOffset(Now);
    
    public DateTimeOffset OffsetUtcNow => new DateTimeOffset(UtcNow);
    
    /// <summary>
    /// Sets the current time for testing
    /// </summary>
    public void SetCurrentTime(DateTime time)
    {
        _currentTime = time;
    }
    
    /// <summary>
    /// Advances the current time by the specified amount
    /// </summary>
    public void AdvanceTime(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }
    
    /// <summary>
    /// Advances the current time by the specified number of days
    /// </summary>
    public void AdvanceDays(int days)
    {
        _currentTime = _currentTime.AddDays(days);
    }
    
    /// <summary>
    /// Advances the current time by the specified number of hours
    /// </summary>
    public void AdvanceHours(int hours)
    {
        _currentTime = _currentTime.AddHours(hours);
    }
    
    /// <summary>
    /// Advances the current time by the specified number of minutes
    /// </summary>
    public void AdvanceMinutes(int minutes)
    {
        _currentTime = _currentTime.AddMinutes(minutes);
    }
}