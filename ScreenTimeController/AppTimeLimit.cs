using System;

namespace ScreenTimeController;

/// <summary>
/// Represents time limit settings for a specific application.
/// </summary>
public class AppTimeLimit
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// This is typically the process name or executable file name.
    /// </summary>
    public string AppIdentifier { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name for the application.
    /// This is the user-friendly name shown in the UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the daily time limit for this application.
    /// </summary>
    public TimeSpan DailyLimit { get; set; } = TimeSpan.Zero;
    
    /// <summary>
    /// Gets or sets a value indicating whether the time limit is enabled for this application.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the path to the application icon.
    /// This can be null if no icon is available.
    /// </summary>
    public string? IconPath { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppTimeLimit"/> class.
    /// </summary>
    public AppTimeLimit() { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AppTimeLimit"/> class with specified values.
    /// </summary>
    /// <param name="appIdentifier">The application identifier (cannot be null or empty).</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="dailyLimit">The daily time limit.</param>
    /// <param name="isEnabled">Whether the limit is enabled.</param>
    /// <param name="iconPath">Optional path to the application icon.</param>
    /// <exception cref="ArgumentException">Thrown when appIdentifier is null or empty.</exception>
    public AppTimeLimit(string appIdentifier, string displayName, TimeSpan dailyLimit, bool isEnabled = true, string? iconPath = null)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            throw new ArgumentException("Application identifier cannot be null or empty.", nameof(appIdentifier));
        }
        AppIdentifier = appIdentifier;
        DisplayName = displayName;
        DailyLimit = dailyLimit;
        IsEnabled = isEnabled;
        IconPath = iconPath;
    }
    
    /// <summary>
    /// Returns a string representation of the application time limit.
    /// </summary>
    /// <returns>A string containing the application name and limit.</returns>
    public override string ToString()
    {
        return $"{DisplayName} ({AppIdentifier}): {DailyLimit.TotalMinutes} minutes - {(IsEnabled ? "Enabled" : "Disabled")}";
    }
    
    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    /// <returns>A new <see cref="AppTimeLimit"/> instance with the same values.</returns>
    public AppTimeLimit Clone()
    {
        return new AppTimeLimit
        {
            AppIdentifier = AppIdentifier,
            DisplayName = DisplayName,
            DailyLimit = DailyLimit,
            IsEnabled = IsEnabled,
            IconPath = IconPath
        };
    }
}
