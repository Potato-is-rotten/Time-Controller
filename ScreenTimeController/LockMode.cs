namespace ScreenTimeController;

/// <summary>
/// Defines the lock mode for screen time control.
/// </summary>
public enum LockMode
{
    /// <summary>
    /// Full screen lock mode - locks the entire screen when time limit is reached.
    /// </summary>
    FullScreen = 0,
    
    /// <summary>
    /// Per-application lock mode - locks specific applications when their time limits are reached.
    /// </summary>
    PerApp = 1
}
