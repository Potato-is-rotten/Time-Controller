using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ScreenTimeController;

public class AppLockService : IDisposable
{
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;
    private readonly Dictionary<string, AppLockWindow> _activeLocks;
    private readonly Dictionary<string, DateTime> _lastLockTime;
    private readonly object _lockObject = new();
    private System.Timers.Timer? _checkTimer;
    private System.Timers.Timer? _foregroundTimer;
    private string? _currentForegroundApp;
    private bool _isDisposed;
    private bool _isEnabled;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// Gets or sets whether the service is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                if (_isEnabled)
                {
                    StartMonitoring();
                }
                else
                {
                    StopMonitoring();
                    CloseAllLocks();
                }
            }
        }
    }

    /// <summary>
    /// Occurs when an application is locked.
    /// </summary>
    public event EventHandler<AppLockedEventArgs>? AppLocked;

    /// <summary>
    /// Occurs when an application is unlocked.
    /// </summary>
    public event EventHandler<AppLockedEventArgs>? AppUnlocked;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockService"/> class.
    /// </summary>
    /// <param name="settingsManager">The settings manager.</param>
    /// <param name="timeTracker">The time tracker.</param>
    public AppLockService(SettingsManager settingsManager, TimeTracker timeTracker)
    {
        _settingsManager = settingsManager;
        _timeTracker = timeTracker;
        _activeLocks = new Dictionary<string, AppLockWindow>();
        _lastLockTime = new Dictionary<string, DateTime>();
        _isDisposed = false;
        _isEnabled = false;

        _timeTracker.AppLimitExceeded += OnAppLimitExceeded;
    }

    /// <summary>
    /// Starts the monitoring service.
    /// </summary>
    public void Start()
    {
        if (_isEnabled) return;

        _isEnabled = true;
        StartMonitoring();
    }

    /// <summary>
    /// Stops the monitoring service.
    /// </summary>
    public void Stop()
    {
        _isEnabled = false;
        StopMonitoring();
        CloseAllLocks();
    }

    private void StartMonitoring()
    {
        if (_checkTimer == null)
        {
            _checkTimer = new System.Timers.Timer(1000.0);
            _checkTimer.Elapsed += OnCheckTimerElapsed;
        }
        _checkTimer.Start();

        if (_foregroundTimer == null)
        {
            _foregroundTimer = new System.Timers.Timer(500.0);
            _foregroundTimer.Elapsed += OnForegroundTimerElapsed;
        }
        _foregroundTimer.Start();
    }

    private void StopMonitoring()
    {
        _checkTimer?.Stop();
        _foregroundTimer?.Stop();
    }

    private void OnCheckTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isEnabled) return;

        try
        {
            CheckAllAppLimits();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CheckAllAppLimits error: {ex.Message}");
        }
    }

    private void OnForegroundTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isEnabled) return;

        try
        {
            string? foregroundApp = GetForegroundApplication();
            if (foregroundApp != _currentForegroundApp)
            {
                _currentForegroundApp = foregroundApp;
                if (!string.IsNullOrEmpty(foregroundApp))
                {
                    CheckAppLimit(foregroundApp);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnForegroundTimerElapsed error: {ex.Message}");
        }
    }

    private void CheckAllAppLimits()
    {
        if (_settingsManager.CurrentLockMode != LockMode.PerApp)
        {
            Debug.WriteLine($"[AppLockService] CheckAllAppLimits: LockMode is {_settingsManager.CurrentLockMode}, not PerApp");
            return;
        }

        List<string> exceededApps = _timeTracker.GetExceededApps();
        Debug.WriteLine($"[AppLockService] CheckAllAppLimits: Found {exceededApps.Count} exceeded apps");

        foreach (string appIdentifier in exceededApps)
        {
            Debug.WriteLine($"[AppLockService] CheckAllAppLimits: App {appIdentifier} exceeded");
            lock (_lockObject)
            {
                if (!_activeLocks.ContainsKey(appIdentifier))
                {
                    ShowLockWindow(appIdentifier);
                }
            }
        }
    }

    private void CheckAppLimit(string appIdentifier)
    {
        if (_settingsManager.CurrentLockMode != LockMode.PerApp)
        {
            return;
        }

        var result = _timeTracker.CheckAppTimeLimitWithResult(appIdentifier);
        
        Debug.WriteLine($"[AppLockService] CheckAppLimit: {appIdentifier}, Limit={result.Limit?.DisplayName}, IsExceeded={result.IsExceeded}, IsCancelled={result.IsCancelled}");
        
        if (result.Limit == null || !result.Limit.IsEnabled) return;

        if (result.IsExceeded && !result.IsCancelled)
        {
            lock (_lockObject)
            {
                if (!_activeLocks.ContainsKey(appIdentifier))
                {
                    int limitMinutes = (int)result.Limit.DailyLimit.TotalMinutes;
                    int usedMinutes = (int)result.UsedTime.TotalMinutes;
                    int exceededMinutes = usedMinutes - limitMinutes;
                    Debug.WriteLine($"[AppLockService] CheckAppLimit: Showing lock window for {appIdentifier}");
                    ShowLockWindow(appIdentifier, limitMinutes, usedMinutes, exceededMinutes, result.Limit);
                }
            }
        }
    }

    private void OnAppLimitExceeded(object? sender, AppLimitExceededEventArgs e)
    {
        if (!_isEnabled) return;
        if (_settingsManager.CurrentLockMode != LockMode.PerApp) return;

        lock (_lockObject)
        {
            if (!_activeLocks.ContainsKey(e.AppIdentifier))
            {
                ShowLockWindow(e.AppIdentifier, e.LimitMinutes, e.UsedMinutes, e.ExceededMinutes, e.LimitInfo!);
            }
        }
    }

    private void ShowLockWindow(string appIdentifier)
    {
        var limit = _settingsManager.GetAppTimeLimit(appIdentifier);
        if (limit == null) return;

        var used = _timeTracker.GetAppUsageToday(appIdentifier);
        int limitMinutes = (int)limit.DailyLimit.TotalMinutes;
        int usedMinutes = (int)used.TotalMinutes;
        int exceededMinutes = usedMinutes - limitMinutes;

        ShowLockWindow(appIdentifier, limitMinutes, usedMinutes, exceededMinutes, limit);
    }

    private void ShowLockWindow(string appIdentifier, int limitMinutes, int usedMinutes, int exceededMinutes, AppTimeLimit limit)
    {
        if (_isDisposed)
        {
            Debug.WriteLine($"[AppLockService] ShowLockWindow: _isDisposed is true, returning");
            return;
        }

        if (_lastLockTime.TryGetValue(appIdentifier, out DateTime lastLock))
        {
            if ((DateTime.Now - lastLock).TotalSeconds < 5)
            {
                Debug.WriteLine($"[AppLockService] ShowLockWindow: Too soon since last lock for {appIdentifier}");
                return;
            }
        }

        string processName = appIdentifier;
        if (appIdentifier.Contains("|"))
        {
            processName = appIdentifier.Split('|')[0];
        }

        bool hasWindow = WindowHelper.ProcessHasWindow(processName);
        Debug.WriteLine($"[AppLockService] ShowLockWindow: ProcessHasWindow({processName}) = {hasWindow}");
        
        _lastLockTime[appIdentifier] = DateTime.Now;

        Process? targetProcess = null;
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                foreach (Process p in processes)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        targetProcess = p;
                        break;
                    }
                }
                if (targetProcess == null && processes.Length > 0)
                {
                    targetProcess = processes[0];
                    Debug.WriteLine($"[AppLockService] ShowLockWindow: Using first process without window handle");
                }
            }
        }
        catch { }

        try
        {
            AppLockWindow? window = null;

            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                mainForm.Invoke(new Action(() =>
                {
                    window = new AppLockWindow(
                        limit.DisplayName,
                        appIdentifier,
                        limitMinutes,
                        usedMinutes,
                        exceededMinutes,
                        limit,
                        targetProcess,
                        _settingsManager,
                        _timeTracker);
                }));
            }
            else
            {
                window = new AppLockWindow(
                    limit.DisplayName,
                    appIdentifier,
                    limitMinutes,
                    usedMinutes,
                    exceededMinutes,
                    limit,
                    targetProcess,
                    _settingsManager,
                    _timeTracker);
            }

            if (window != null)
            {
                lock (_lockObject)
                {
                    _activeLocks[appIdentifier] = window;
                }

                window.FormClosed += (s, e) =>
                {
                    lock (_lockObject)
                    {
                        _activeLocks.Remove(appIdentifier);
                    }
                    AppUnlocked?.Invoke(this, new AppLockedEventArgs(appIdentifier, limit));
                };

                AppLocked?.Invoke(this, new AppLockedEventArgs(appIdentifier, limit));

                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    mainForm.Invoke(new Action(() => window.Show()));
                }
                else
                {
                    window.Show();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ShowLockWindow error: {ex.Message}");
        }
    }

    private void CloseAllLocks()
    {
        lock (_lockObject)
        {
            foreach (var kvp in _activeLocks)
            {
                try
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0];
                        mainForm.Invoke(new Action(() => kvp.Value.Close()));
                    }
                    else
                    {
                        kvp.Value.Close();
                    }
                }
                catch { }
            }
            _activeLocks.Clear();
        }
    }

    private string? GetForegroundApplication()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return null;

        uint processId;
        GetWindowThreadProcessId(hwnd, out processId);

        if (processId == 0) return null;

        try
        {
            Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all currently locked applications.
    /// </summary>
    /// <returns>List of locked application identifiers.</returns>
    public List<string> GetLockedApps()
    {
        lock (_lockObject)
        {
            return new List<string>(_activeLocks.Keys);
        }
    }

    /// <summary>
    /// Unlocks a specific application.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    public void UnlockApp(string appIdentifier)
    {
        lock (_lockObject)
        {
            if (_activeLocks.TryGetValue(appIdentifier, out AppLockWindow? window))
            {
                window.CanDismiss = true;
                try
                {
                    if (Application.OpenForms.Count > 0)
                    {
                        var mainForm = Application.OpenForms[0];
                        mainForm.Invoke(new Action(() => window.Close()));
                    }
                    else
                    {
                        window.Close();
                    }
                }
                catch { }
                _activeLocks.Remove(appIdentifier);
            }
        }
    }

    /// <summary>
    /// Checks if an application is currently locked.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>True if the application is locked.</returns>
    public bool IsAppLocked(string appIdentifier)
    {
        lock (_lockObject)
        {
            return _activeLocks.ContainsKey(appIdentifier);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            StopMonitoring();

            if (_timeTracker != null)
            {
                _timeTracker.AppLimitExceeded -= OnAppLimitExceeded;
            }

            CloseAllLocks();

            _checkTimer?.Dispose();
            _foregroundTimer?.Dispose();
        }
    }
}

/// <summary>
/// Event arguments for app locked events.
/// </summary>
public class AppLockedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the application identifier.
    /// </summary>
    public string AppIdentifier { get; }

    /// <summary>
    /// Gets the application time limit.
    /// </summary>
    public AppTimeLimit Limit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockedEventArgs"/> class.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <param name="limit">The application time limit.</param>
    public AppLockedEventArgs(string appIdentifier, AppTimeLimit limit)
    {
        AppIdentifier = appIdentifier;
        Limit = limit;
    }
}
