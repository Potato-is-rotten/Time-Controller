using System;

namespace ScreenTimeController;

/// <summary>
/// Represents a session of application usage for time tracking.
/// </summary>
public class AppLockSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    public string AppIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the start time of this usage session.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets or sets the accumulated time for this session.
    /// </summary>
    public TimeSpan AccumulatedTime { get; set; } = TimeSpan.Zero;
    
    /// <summary>
    /// Gets or sets the end time of this usage session.
    /// This is null if the session is still active.
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Gets a value indicating whether this session is currently active.
    /// </summary>
    public bool IsActive => EndTime == null;
    
    /// <summary>
    /// Gets the total duration of this session.
    /// If the session is active, returns the time from start to now.
    /// </summary>
    public TimeSpan Duration
    {
        get
        {
            if (EndTime.HasValue)
            {
                return EndTime.Value - StartTime;
            }
            return DateTime.Now - StartTime;
        }
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockSession"/> class.
    /// </summary>
    public AppLockSession() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockSession"/> class with specified application identifier.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    public AppLockSession(string appIdentifier)
    {
        AppIdentifier = appIdentifier;
        StartTime = DateTime.Now;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockSession"/> class with specified values.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="startTime">The session start time.</param>
    /// <param name="accumulatedTime">The accumulated time.</param>
    public AppLockSession(string appIdentifier, DateTime startTime, TimeSpan accumulatedTime)
    {
        AppIdentifier = appIdentifier;
        StartTime = startTime;
        AccumulatedTime = accumulatedTime;
    }
    
    /// <summary>
    /// Ends this session and sets the end time to now.
    /// </summary>
    public void EndSession()
    {
        if (IsActive)
        {
            EndTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Adds time to the accumulated time for this session.
    /// </summary>
    /// <param name="duration">The duration to add.</param>
    public void AddTime(TimeSpan duration)
    {
        if (duration > TimeSpan.Zero)
        {
            AccumulatedTime += duration;
        }
    }
    
    /// <summary>
    /// Returns a string representation of this session.
    /// </summary>
    /// <returns>A string containing session details.</returns>
    public override string ToString()
    {
        string status = IsActive ? "Active" : "Ended";
        return $"{AppIdentifier}: {StartTime:HH:mm:ss} - {Duration.TotalMinutes:F1} min ({status})";
    }
}
