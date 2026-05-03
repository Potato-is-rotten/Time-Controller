using System;

namespace ScreenTimeController;

/// <summary>
/// Event arguments for application time limit exceeded event.
/// </summary>
public class AppLimitExceededEventArgs : EventArgs
{
    /// <summary>
    /// Gets the application identifier (process name).
    /// </summary>
    public string AppIdentifier { get; }
    
    /// <summary>
    /// Gets the daily limit in minutes.
    /// </summary>
    public int LimitMinutes { get; }
    
    /// <summary>
    /// Gets the used time in minutes.
    /// </summary>
    public int UsedMinutes { get; }
    
    /// <summary>
    /// Gets the exceeded time in minutes.
    /// </summary>
    public int ExceededMinutes { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the lock should be cancelled.
    /// Set to true to prevent the application from being locked.
    /// </summary>
    public bool Cancel { get; set; }
    
    /// <summary>
    /// Gets the application time limit information.
    /// </summary>
    public AppTimeLimit? LimitInfo { get; }
    
    /// <summary>
    /// Gets the time when the limit was exceeded.
    /// </summary>
    public DateTime ExceededTime { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppLimitExceededEventArgs"/> class.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="limitMinutes">The daily limit in minutes.</param>
    /// <param name="usedMinutes">The used time in minutes.</param>
    /// <param name="exceededMinutes">The exceeded time in minutes.</param>
    /// <param name="limitInfo">The application time limit information.</param>
    public AppLimitExceededEventArgs(string appIdentifier, int limitMinutes, int usedMinutes, int exceededMinutes, AppTimeLimit? limitInfo = null)
    {
        AppIdentifier = appIdentifier;
        LimitMinutes = limitMinutes;
        UsedMinutes = usedMinutes;
        ExceededMinutes = exceededMinutes;
        LimitInfo = limitInfo;
        ExceededTime = DateTime.Now;
        Cancel = false;
    }
    
    /// <summary>
    /// Returns a string representation of the event arguments.
    /// </summary>
    /// <returns>A string containing event details.</returns>
    public override string ToString()
    {
        return $"{AppIdentifier}: {UsedMinutes}/{LimitMinutes} min (exceeded by {ExceededMinutes} min) - {(Cancel ? "Cancelled" : "Will Lock")}";
    }
}
