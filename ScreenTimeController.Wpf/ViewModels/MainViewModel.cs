using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ScreenTimeController.Wpf.ViewModels;

public class AppUsageItem : ViewModelBase
{
    public string AppName { get; set; } = "";
    
    private TimeSpan _usageTime;
    public TimeSpan UsageTime
    {
        get => _usageTime;
        set
        {
            if (SetProperty(ref _usageTime, value))
            {
                OnPropertyChanged(nameof(UsageTimeDisplay));
            }
        }
    }
    
    public string UsageTimeDisplay => $"{UsageTime.Hours}h {UsageTime.Minutes}m {UsageTime.Seconds}s";
    public string? IconPath { get; set; }
}

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _trackingTimer;
    private readonly DispatcherTimer _lockCheckTimer;
    private readonly DispatcherTimer _protectionStatusTimer;
    
    private bool _isLocked;
    private bool _hasWarned5Minutes;
    private bool _isDisposed;
    private bool _hasShownMinimizedTip;
    private int _selectedTabIndex;
    private string _dailyLimitText = "";
    private string _usedTodayText = "";
    private string _remainingText = "";
    private int _progressValue;
    private string _abnormalExitsText = "";
    private string _serviceStatusText = "";
    private string _taskStatusText = "";
    private bool _isServiceRunning;
    private bool _isTaskInstalled;

    public ObservableCollection<AppUsageItem> AppUsageList { get; } = new();

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                if (value == 2)
                {
                    RequestProtectionAccess?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    public string DailyLimitText
    {
        get => _dailyLimitText;
        set => SetProperty(ref _dailyLimitText, value);
    }

    public string UsedTodayText
    {
        get => _usedTodayText;
        set => SetProperty(ref _usedTodayText, value);
    }

    public string RemainingText
    {
        get => _remainingText;
        set => SetProperty(ref _remainingText, value);
    }

    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string AbnormalExitsText
    {
        get => _abnormalExitsText;
        set => SetProperty(ref _abnormalExitsText, value);
    }

    public string ServiceStatusText
    {
        get => _serviceStatusText;
        set => SetProperty(ref _serviceStatusText, value);
    }

    public string TaskStatusText
    {
        get => _taskStatusText;
        set => SetProperty(ref _taskStatusText, value);
    }

    public bool IsServiceRunning
    {
        get => _isServiceRunning;
        set => SetProperty(ref _isServiceRunning, value);
    }

    public bool IsTaskInstalled
    {
        get => _isTaskInstalled;
        set => SetProperty(ref _isTaskInstalled, value);
    }

    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ShowWindowCommand { get; }

    public event EventHandler? RequestSettings;
    public event EventHandler? RequestExit;
    public event EventHandler? RequestShowWindow;
    public event EventHandler? RequestUnlock;
    public event EventHandler? RequestProtectionAccess;
    public event EventHandler? MinimizeToTray;

    public MainViewModel(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _timeTracker = new TimeTracker(_settingsManager);
        
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _uiTimer.Tick += OnUITimerTick;
        
        _trackingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _trackingTimer.Tick += OnTrackingTimerTick;
        
        _lockCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lockCheckTimer.Tick += OnLockCheckTick;
        
        _protectionStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _protectionStatusTimer.Tick += OnProtectionStatusTimerTick;

        SettingsCommand = new RelayCommand(ExecuteSettings);
        ExitCommand = new RelayCommand(ExecuteExit);
        ShowWindowCommand = new RelayCommand(ExecuteShowWindow);

        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    public void Start()
    {
        _uiTimer.Start();
        _trackingTimer.Start();
        _lockCheckTimer.Start();
        _protectionStatusTimer.Start();

        UpdateUI();
        UpdateProtectionStatus();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateUI();
        UpdateProtectionStatus();
    }

    private void OnUITimerTick(object? sender, EventArgs e)
    {
        if (!_isDisposed)
        {
            UpdateUI();
        }
    }

    private void OnTrackingTimerTick(object? sender, EventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            string? activeWindowProcessName = WindowHelper.GetActiveWindowProcessName();
            if (!string.IsNullOrEmpty(activeWindowProcessName))
            {
                _timeTracker.RecordUsage(TimeSpan.FromSeconds(2.0), activeWindowProcessName);
            }
            CheckTimeLimit();
        }
        catch { }
    }

    private void OnLockCheckTick(object? sender, EventArgs e)
    {
        if (_isLocked)
        {
            RequestUnlock?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnProtectionStatusTimerTick(object? sender, EventArgs e)
    {
        UpdateProtectionStatus();
    }

    private void UpdateUI()
    {
        if (_isDisposed) return;

        try
        {
            TimeSpan dailyLimit = _timeTracker.GetDailyLimit();
            TimeSpan totalUsage = _timeTracker.TotalUsage;
            TimeSpan bonusTime = _timeTracker.BonusTime;
            TimeSpan actualRemaining = dailyLimit - totalUsage;
            if (actualRemaining < TimeSpan.Zero)
            {
                actualRemaining = TimeSpan.Zero;
            }

            DailyLimitText = $"{LanguageManager.GetString("DailyLimit")}: {dailyLimit.Hours}h {dailyLimit.Minutes}m";
            UsedTodayText = $"{LanguageManager.GetString("UsedToday")}: {totalUsage.Hours}h {totalUsage.Minutes}m";
            
            if (bonusTime > TimeSpan.Zero)
            {
                RemainingText = $"{LanguageManager.GetString("Remaining")}: {actualRemaining.Hours}h {actualRemaining.Minutes}m (+{(int)bonusTime.TotalMinutes}m bonus)";
            }
            else
            {
                RemainingText = $"{LanguageManager.GetString("Remaining")}: {actualRemaining.Hours}h {actualRemaining.Minutes}m";
            }

            int progress = 0;
            if (dailyLimit.TotalSeconds > 0)
            {
                progress = (int)(totalUsage.TotalSeconds / dailyLimit.TotalSeconds * 100.0);
                if (progress > 100) progress = 100;
            }
            ProgressValue = progress;

            var appUsage = _timeTracker.AppUsage;
            bool needsRefresh = AppUsageList.Count != appUsage.Count;
            
            if (!needsRefresh)
            {
                int idx = 0;
                foreach (var item in appUsage)
                {
                    if (idx >= AppUsageList.Count || AppUsageList[idx].AppName != item.Key)
                    {
                        needsRefresh = true;
                        break;
                    }
                    idx++;
                }
            }

            if (needsRefresh)
            {
                AppUsageList.Clear();
                foreach (var item in appUsage)
                {
                    AppUsageList.Add(new AppUsageItem
                    {
                        AppName = item.Key,
                        UsageTime = item.Value
                    });
                }
            }
            else
            {
                int idx = 0;
                foreach (var item in appUsage)
                {
                    if (idx < AppUsageList.Count)
                    {
                        AppUsageList[idx].UsageTime = item.Value;
                    }
                    idx++;
                }
            }
        }
        catch { }
    }

    private void CheckTimeLimit()
    {
        try
        {
            TimeSpan dailyLimit = _timeTracker.GetDailyLimit();
            TimeSpan totalUsage = _timeTracker.TotalUsage;
            TimeSpan bonusTime = _timeTracker.BonusTime;
            TimeSpan remaining = dailyLimit + bonusTime - totalUsage;

            if (remaining <= TimeSpan.Zero && !_isLocked)
            {
                _isLocked = true;
                LockScreen();
            }
            else if (remaining <= TimeSpan.FromMinutes(5) && remaining > TimeSpan.Zero && !_hasWarned5Minutes)
            {
                _hasWarned5Minutes = true;
                Show5MinuteWarning();
            }
        }
        catch { }
    }

    private void LockScreen()
    {
        try
        {
            WindowHelper.LockWorkStation();
        }
        catch { }
    }

    private void Show5MinuteWarning()
    {
        System.Windows.MessageBox.Show(
            LanguageManager.GetString("FiveMinutesRemaining"),
            LanguageManager.GetString("ScreenTimeWarning"),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void UpdateProtectionStatus()
    {
        if (_isDisposed) return;

        try
        {
            int abnormalCount = AbnormalExitTracker.GetTodayAbnormalExitCount();
            AbnormalExitsText = string.Format(LanguageManager.GetString("AbnormalExitCount"), abnormalCount);
        }
        catch
        {
            AbnormalExitsText = LanguageManager.GetString("AbnormalExitCount").Replace("{0}", "0");
        }

        try
        {
            IsTaskInstalled = TaskSchedulerManager.IsTaskInstalled();
            TaskStatusText = IsTaskInstalled
                ? LanguageManager.GetString("TaskInstalled")
                : LanguageManager.GetString("TaskNotInstalled");
        }
        catch
        {
            IsTaskInstalled = false;
            TaskStatusText = LanguageManager.GetString("TaskNotInstalled");
        }

        try
        {
            IsServiceRunning = IsServiceRunningCheck();
            ServiceStatusText = IsServiceRunning
                ? LanguageManager.GetString("ServiceRunning")
                : LanguageManager.GetString("ServiceStopped");
        }
        catch
        {
            IsServiceRunning = false;
            ServiceStatusText = LanguageManager.GetString("ServiceStopped");
        }
    }

    private static bool IsServiceRunningCheck()
    {
        try
        {
            Process[] processes = Process.GetProcessesByName("ProtectionService");
            bool running = processes.Length > 0;
            foreach (Process p in processes)
            {
                p.Dispose();
            }
            return running;
        }
        catch
        {
            return false;
        }
    }

    public void OnUnlockSuccess()
    {
        _isLocked = false;
        _hasWarned5Minutes = false;
    }

    public void OnWindowMinimized()
    {
        if (!_hasShownMinimizedTip)
        {
            MinimizeToTray?.Invoke(this, EventArgs.Empty);
            _hasShownMinimizedTip = true;
        }
    }

    private void ExecuteSettings(object? parameter)
    {
        RequestSettings?.Invoke(this, EventArgs.Empty);
        _hasWarned5Minutes = false;
        UpdateUI();
    }

    private void ExecuteExit(object? parameter)
    {
        RequestExit?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteShowWindow(object? parameter)
    {
        RequestShowWindow?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        LanguageManager.LanguageChanged -= OnLanguageChanged;

        _uiTimer.Stop();
        _trackingTimer.Stop();
        _lockCheckTimer.Stop();
        _protectionStatusTimer.Stop();

        _timeTracker.MarkCleanExit();
        _timeTracker.ForceSave();
        _timeTracker.Dispose();
    }

    public bool VerifyExitPassword()
    {
        return !_settingsManager.HasPassword();
    }
}
