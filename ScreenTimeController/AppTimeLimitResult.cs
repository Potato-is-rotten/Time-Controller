using System;

namespace ScreenTimeController;

/// <summary>
/// Represents the result of checking an application's time limit.
/// </summary>
public class AppTimeLimitResult
{
    /// <summary>
    /// Gets a value indicating whether the time limit has been exceeded.
    /// </summary>
    public bool IsExceeded { get; }
    
    /// <summary>
    /// Gets the remaining time before the limit is reached.
    /// Returns TimeSpan.MaxValue if no limit is set.
    /// Returns TimeSpan.Zero if the limit has been exceeded.
    /// </summary>
    public TimeSpan RemainingTime { get; }
    
    /// <summary>
    /// Gets the application time limit information.
    /// Returns null if no limit is configured for the application.
    /// </summary>
    public AppTimeLimit? Limit { get; }
    
    /// <summary>
    /// Gets a value indicating whether the lock was cancelled by an event handler.
    /// </summary>
    public bool IsCancelled { get; }
    
    /// <summary>
    /// Gets the application identifier that was checked.
    /// </summary>
    public string AppIdentifier { get; }
    
    /// <summary>
    /// Gets the total time used today for this application.
    /// </summary>
    public TimeSpan UsedTime { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppTimeLimitResult"/> class.
    /// </summary>
    /// <param name="isExceeded">Whether the limit is exceeded.</param>
    /// <param name="remainingTime">The remaining time.</param>
    /// <param name="limit">The limit information.</param>
    /// <param name="isCancelled">Whether the lock was cancelled.</param>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="usedTime">The used time.</param>
    private AppTimeLimitResult(bool isExceeded, TimeSpan remainingTime, AppTimeLimit? limit, bool isCancelled, string appIdentifier, TimeSpan usedTime)
    {
        IsExceeded = isExceeded;
        RemainingTime = remainingTime;
        Limit = limit;
        IsCancelled = isCancelled;
        AppIdentifier = appIdentifier;
        UsedTime = usedTime;
    }
    
    /// <summary>
    /// Creates a result indicating no limit is configured.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>A result with no limit.</returns>
    public static AppTimeLimitResult NoLimit(string appIdentifier)
    {
        return new AppTimeLimitResult(false, TimeSpan.MaxValue, null, false, appIdentifier, TimeSpan.Zero);
    }
    
    /// <summary>
    /// Creates a result indicating the limit has not been exceeded.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="remainingTime">The remaining time.</param>
    /// <param name="limit">The limit information.</param>
    /// <param name="usedTime">The used time.</param>
    /// <returns>A result with remaining time.</returns>
    public static AppTimeLimitResult WithinLimit(string appIdentifier, TimeSpan remainingTime, AppTimeLimit limit, TimeSpan usedTime)
    {
        return new AppTimeLimitResult(false, remainingTime, limit, false, appIdentifier, usedTime);
    }
    
    /// <summary>
    /// Creates a result indicating the limit has been exceeded.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="limit">The limit information.</param>
    /// <param name="usedTime">The used time.</param>
    /// <param name="isCancelled">Whether the lock was cancelled.</param>
    /// <returns>A result indicating exceeded limit.</returns>
    public static AppTimeLimitResult Exceeded(string appIdentifier, AppTimeLimit limit, TimeSpan usedTime, bool isCancelled = false)
    {
        return new AppTimeLimitResult(!isCancelled, TimeSpan.Zero, limit, isCancelled, appIdentifier, usedTime);
    }
    
    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns>A string describing the result.</returns>
    public override string ToString()
    {
        if (Limit == null)
        {
            return $"{AppIdentifier}: No limit configured";
        }
        
        if (IsCancelled)
        {
            return $"{AppIdentifier}: Exceeded but cancelled (Used: {UsedTime.TotalMinutes:F0}min, Limit: {Limit.DailyLimit.TotalMinutes:F0}min)";
        }
        
        if (IsExceeded)
        {
            return $"{AppIdentifier}: Exceeded (Used: {UsedTime.TotalMinutes:F0}min, Limit: {Limit.DailyLimit.TotalMinutes:F0}min)";
        }
        
        return $"{AppIdentifier}: {RemainingTime.TotalMinutes:F0}min remaining (Used: {UsedTime.TotalMinutes:F0}min, Limit: {Limit.DailyLimit.TotalMinutes:F0}min)";
    }
}
