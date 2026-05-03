using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ScreenTimeController;

public class TimeTracker : IDisposable
{
    private const string UsageFileName = "usage.txt";
    private const string AppUsageFileName = "app_usage.txt";
    
    /// <summary>
    /// Occurs when an application's time limit is exceeded.
    /// </summary>
    public event EventHandler<AppLimitExceededEventArgs>? AppLimitExceeded;
    
    private TimeSpan _totalUsage;
    private TimeSpan _bonusTime;
    private readonly Dictionary<string, TimeSpan> _appUsage;
    private readonly SettingsManager _settingsManager;
    private readonly string _dataDirectory;
    private readonly string _usageFilePath;
    private readonly string _appUsageFilePath;
    private readonly string _backupFilePath;
    private readonly string _cleanExitFilePath;
    private System.Timers.Timer? _midnightTimer;
    private System.Timers.Timer? _saveTimer;
    private System.Timers.Timer? _integrityCheckTimer;
    private DateTime _lastCheckedDate;
    private readonly object _lockObject = new();
    private readonly object _saveLock = new();
    private bool _isDisposed;
    private bool _needsSave;

    public TimeSpan TotalUsage
    {
        get { lock (_lockObject) { return _totalUsage; } }
    }

    public TimeSpan BonusTime
    {
        get { lock (_lockObject) { return _bonusTime; } }
    }

    public TimeSpan EffectiveUsage
    {
        get { lock (_lockObject) { return _totalUsage - _bonusTime; } }
    }

    public Dictionary<string, TimeSpan> AppUsage
    {
        get { lock (_lockObject) { return new Dictionary<string, TimeSpan>(_appUsage); } }
    }

    public TimeTracker(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _appUsage = new Dictionary<string, TimeSpan>();
        _bonusTime = TimeSpan.Zero;
        _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
        _usageFilePath = Path.Combine(_dataDirectory, "usage.txt");
        _appUsageFilePath = Path.Combine(_dataDirectory, "app_usage.txt");
        _backupFilePath = Path.Combine(_dataDirectory, "usage_backup.txt");
        _cleanExitFilePath = GetCleanExitFilePath();
        _lastCheckedDate = DateTime.Today;
        _isDisposed = false;
        _needsSave = false;

        DataProtectionManager.EnsureDirectoryExists();
        LoadAllData();
        CheckAndApplyExitPenalty();
        SetupMidnightTimer();
        SetupSaveTimer();
        SetupIntegrityCheckTimer();
    }

    private static string GetCleanExitFilePath()
    {
        string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
        try
        {
            if (!Directory.Exists(commonDataDir))
            {
                Directory.CreateDirectory(commonDataDir);
            }
        }
        catch { }
        return Path.Combine(commonDataDir, "clean_exit.txt");
    }

    private void EnsureDataDirectory()
    {
        try
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }
        catch { }
    }

    private void SetupSaveTimer()
    {
        _saveTimer = new System.Timers.Timer(10000.0);
        _saveTimer.Elapsed += OnSaveTimerTick;
        _saveTimer.Start();
    }

    private void OnSaveTimerTick(object? sender, ElapsedEventArgs e)
    {
        if (_needsSave)
        {
            SaveAllData();
            _needsSave = false;
        }
    }

    private void SetupMidnightTimer()
    {
        _midnightTimer = new System.Timers.Timer(60000.0);
        _midnightTimer.Elapsed += OnMidnightCheck;
        _midnightTimer.Start();
    }

    private void OnMidnightCheck(object? sender, ElapsedEventArgs e)
    {
        CheckForNewDay();
    }

    private void SetupIntegrityCheckTimer()
    {
        _integrityCheckTimer = new System.Timers.Timer(30000.0);
        _integrityCheckTimer.Elapsed += OnIntegrityCheck;
        _integrityCheckTimer.Start();
    }

    private void OnIntegrityCheck(object? sender, ElapsedEventArgs e)
    {
        CheckDataIntegrity();
    }

    private void CheckDataIntegrity()
    {
        try
        {
            if (!DataProtectionManager.VerifyIntegrity(UsageFileName))
            {
                string? content = DataProtectionManager.LoadWithProtection(UsageFileName);
                if (!string.IsNullOrEmpty(content))
                {
                    DataProtectionManager.RecordTampering(UsageFileName);
                }
            }
            
            if (!DataProtectionManager.VerifyIntegrity(AppUsageFileName))
            {
                string? content = DataProtectionManager.LoadWithProtection(AppUsageFileName);
                if (!string.IsNullOrEmpty(content))
                {
                    DataProtectionManager.RecordTampering(AppUsageFileName);
                }
            }
        }
        catch { }
    }

    private void CheckForNewDay()
    {
        DateTime today = DateTime.Today;
        if (today != _lastCheckedDate)
        {
            SaveAllData();
            lock (_lockObject)
            {
                _totalUsage = TimeSpan.Zero;
                _bonusTime = TimeSpan.Zero;
                _appUsage.Clear();
                _lastCheckedDate = today;
            }
            SaveAllData();
        }
    }

    private void LoadAllData()
    {
        lock (_lockObject)
        {
            try
            {
                LoadUsageData();
                LoadAppUsageData();
            }
            catch { }
        }
    }

    private void LoadUsageData()
    {
        string? content = DataProtectionManager.LoadWithProtection(UsageFileName);
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        string[] parts = content.Split('|');
        if (parts.Length < 2)
        {
            return;
        }

        if (!DateTime.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate))
        {
            return;
        }

        if (fileDate.Date == DateTime.Today)
        {
            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double todayMinutes))
            {
                _totalUsage = TimeSpan.FromMinutes(todayMinutes);
            }

            if (parts.Length >= 3 && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double bonusMinutes))
            {
                _bonusTime = TimeSpan.FromMinutes(bonusMinutes);
            }
        }
        else if (fileDate.Date == DateTime.Today.AddDays(-1))
        {
            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double yesterdayMinutes))
            {
                _totalUsage = TimeSpan.FromMinutes(yesterdayMinutes);
            }

            if (parts.Length >= 3 && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double bonusMinutes))
            {
                _bonusTime = TimeSpan.FromMinutes(bonusMinutes);
            }
        }
    }

    private void LoadAppUsageData()
    {
        string? content = DataProtectionManager.LoadWithDecryption(AppUsageFileName);
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return;
        }

        string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
        string yesterdayStr = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        string? firstLine = lines[0];

        if (!firstLine.StartsWith(todayStr) && !firstLine.StartsWith(yesterdayStr))
        {
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split('|');
            if (parts.Length == 2)
            {
                string appName = parts[0];
                if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double minutes))
                {
                    _appUsage[appName] = TimeSpan.FromMinutes(minutes);
                }
            }
        }
    }

    private string? SafeReadFile(string filePath)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private void SafeWriteFile(string filePath, string content)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try { Directory.CreateDirectory(directory); } catch { }
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                lock (_saveLock)
                {
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); } catch { }
                    }
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                }
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException)
            {
                try
                {
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                    return;
                }
                catch { Thread.Sleep(100); }
            }
            catch
            {
                break;
            }
        }
    }

    public void RecordUsage(TimeSpan duration, string appName = "Unknown")
    {
        if (duration <= TimeSpan.Zero || string.IsNullOrEmpty(appName))
        {
            return;
        }

        lock (_lockObject)
        {
            _totalUsage += duration;
            if (_appUsage.ContainsKey(appName))
            {
                _appUsage[appName] += duration;
            }
            else
            {
                _appUsage[appName] = duration;
            }
        }
        _needsSave = true;
    }

    public void ForceSave()
    {
        SaveAllData();
        _needsSave = false;
    }

    public void AddBonusTime(TimeSpan bonus)
    {
        if (bonus <= TimeSpan.Zero)
        {
            return;
        }

        lock (_lockObject)
        {
            _bonusTime += bonus;
        }
        _needsSave = true;
    }

    public void UseBonusTime(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_bonusTime >= duration)
            {
                _bonusTime -= duration;
            }
            else
            {
                _bonusTime = TimeSpan.Zero;
            }
        }
        _needsSave = true;
    }

    private void SaveAllData()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                SaveUsageData();
                SaveAppUsageData();
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveAllData attempt {attempt + 1} failed: {ex.Message}");
                Thread.Sleep(100);
            }
        }
    }

    private void SaveUsageData()
    {
        string content;
        lock (_lockObject)
        {
            content = string.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd}|{1}|{2}",
                DateTime.Today,
                _totalUsage.TotalMinutes,
                _bonusTime.TotalMinutes);
        }

        if (File.Exists(_usageFilePath))
        {
            try
            {
                File.Copy(_usageFilePath, _backupFilePath, true);
            }
            catch { }
        }

        DataProtectionManager.SaveFast(UsageFileName, content);
    }

    private void SaveAppUsageData()
    {
        string content;
        lock (_lockObject)
        {
            StringBuilder sb = new();
            sb.AppendLine(DateTime.Today.ToString("yyyy-MM-dd"));
            foreach (var kvp in _appUsage)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}|{1}", kvp.Key, kvp.Value.TotalMinutes));
            }
            content = sb.ToString();
        }

        DataProtectionManager.SaveWithEncryption(AppUsageFileName, content);
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _totalUsage = TimeSpan.Zero;
            _bonusTime = TimeSpan.Zero;
            _appUsage.Clear();
        }
        SaveAllData();
    }

    public TimeSpan GetDailyLimit()
    {
        return _settingsManager.GetDailyLimit();
    }

    /// <summary>
    /// Gets the usage time for a specific application today.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>The total usage time for the application today.</returns>
    public TimeSpan GetAppUsageToday(string appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return TimeSpan.Zero;
        }

        lock (_lockObject)
        {
            if (_appUsage.TryGetValue(appIdentifier, out TimeSpan usage))
            {
                return usage;
            }
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the remaining time for a specific application today.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>The remaining time, or TimeSpan.MaxValue if no limit is set.</returns>
    public TimeSpan GetRemainingTime(string appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return TimeSpan.MaxValue;
        }

        var limit = _settingsManager.GetAppTimeLimit(appIdentifier);
        if (limit == null || !limit.IsEnabled)
        {
            return TimeSpan.MaxValue;
        }

        TimeSpan used = GetAppUsageToday(appIdentifier);
        TimeSpan remaining = limit.DailyLimit - used;
        
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Checks if an application has exceeded its time limit.
    /// Triggers AppLimitExceeded event when limit is exceeded.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>A tuple containing: whether time is exceeded, remaining time, and the limit info.</returns>
    public (bool IsExceeded, TimeSpan RemainingTime, AppTimeLimit? Limit) CheckAppTimeLimit(string appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return (false, TimeSpan.MaxValue, null);
        }

        var limit = _settingsManager.GetAppTimeLimit(appIdentifier);
        if (limit == null || !limit.IsEnabled)
        {
            return (false, TimeSpan.MaxValue, null);
        }

        TimeSpan used = GetAppUsageToday(appIdentifier);
        TimeSpan remaining = limit.DailyLimit - used;
        bool isExceeded = remaining <= TimeSpan.Zero;

        if (isExceeded)
        {
            int limitMinutes = (int)limit.DailyLimit.TotalMinutes;
            int usedMinutes = (int)used.TotalMinutes;
            int exceededMinutes = usedMinutes - limitMinutes;
            
            var eventArgs = new AppLimitExceededEventArgs(
                appIdentifier,
                limitMinutes,
                usedMinutes,
                exceededMinutes,
                limit);
            
            AppLimitExceeded?.Invoke(this, eventArgs);
            
            if (eventArgs.Cancel)
            {
                return (false, TimeSpan.Zero, limit);
            }
        }

        return (isExceeded, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, limit);
    }

    /// <summary>
    /// Checks if an application has exceeded its time limit and returns a detailed result.
    /// Triggers AppLimitExceeded event when limit is exceeded.
    /// </summary>
    /// <param name="appIdentifier">The application identifier.</param>
    /// <returns>An <see cref="AppTimeLimitResult"/> containing detailed limit check information.</returns>
    public AppTimeLimitResult CheckAppTimeLimitWithResult(string appIdentifier)
    {
        if (string.IsNullOrEmpty(appIdentifier))
        {
            return AppTimeLimitResult.NoLimit(appIdentifier ?? string.Empty);
        }

        var limit = _settingsManager.GetAppTimeLimit(appIdentifier);
        if (limit == null || !limit.IsEnabled)
        {
            return AppTimeLimitResult.NoLimit(appIdentifier);
        }

        TimeSpan used = GetAppUsageToday(appIdentifier);
        TimeSpan remaining = limit.DailyLimit - used;
        bool isExceeded = remaining <= TimeSpan.Zero;

        if (isExceeded)
        {
            int limitMinutes = (int)limit.DailyLimit.TotalMinutes;
            int usedMinutes = (int)used.TotalMinutes;
            int exceededMinutes = usedMinutes - limitMinutes;
            
            var eventArgs = new AppLimitExceededEventArgs(
                appIdentifier,
                limitMinutes,
                usedMinutes,
                exceededMinutes,
                limit);
            
            AppLimitExceeded?.Invoke(this, eventArgs);
            
            return AppTimeLimitResult.Exceeded(appIdentifier, limit, used, eventArgs.Cancel);
        }

        return AppTimeLimitResult.WithinLimit(appIdentifier, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, limit, used);
    }

    /// <summary>
    /// Gets all applications that have exceeded their time limits.
    /// </summary>
    /// <returns>A list of application identifiers that have exceeded limits.</returns>
    public List<string> GetExceededApps()
    {
        var result = new List<string>();
        var limits = _settingsManager.AppTimeLimits;

        foreach (var limit in limits)
        {
            if (limit.IsEnabled)
            {
                var (isExceeded, _, _) = CheckAppTimeLimit(limit.AppIdentifier);
                if (isExceeded)
                {
                    result.Add(limit.AppIdentifier);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the current lock mode.
    /// </summary>
    /// <returns>The current lock mode.</returns>
    public LockMode GetCurrentLockMode()
    {
        return _settingsManager.CurrentLockMode;
    }

    private void CheckAndApplyExitPenalty()
    {
        try
        {
            string? usageContent = SafeReadFile(_usageFilePath);
            if (string.IsNullOrEmpty(usageContent))
            {
                return;
            }

            string[] usageData = usageContent.Split('|');
            if (usageData.Length < 2)
            {
                return;
            }

            if (!DateTime.TryParseExact(usageData[0], "yyyy-MM-dd", null, DateTimeStyles.None, out DateTime usageDate))
            {
                return;
            }

            if (usageDate.Date != DateTime.Today)
            {
                return;
            }

            bool wasCleanExit = false;
            string? exitContent = SafeReadFile(_cleanExitFilePath);
            if (!string.IsNullOrEmpty(exitContent) && exitContent.StartsWith(DateTime.Today.ToString("yyyy-MM-dd")))
            {
                wasCleanExit = true;
            }

            try
            {
                if (File.Exists(_cleanExitFilePath))
                {
                    File.Delete(_cleanExitFilePath);
                }
            }
            catch { }

            if (!wasCleanExit)
            {
                lock (_lockObject)
                {
                    _totalUsage += TimeSpan.FromMinutes(1);
                }
                AbnormalExitTracker.IncrementAbnormalExitCount();
                _needsSave = true;
            }
        }
        catch { }
    }

    public void MarkCleanExit()
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
                if (!Directory.Exists(commonDataDir))
                {
                    Directory.CreateDirectory(commonDataDir);
                }
                
                string content = DateTime.Today.ToString("yyyy-MM-dd");
                SafeWriteFile(_cleanExitFilePath, content);
                
                string? verify = SafeReadFile(_cleanExitFilePath);
                if (verify?.Trim() == content)
                {
                    return;
                }
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            try
            {
                _midnightTimer?.Stop();
                _midnightTimer?.Dispose();
            }
            catch { }

            try
            {
                _saveTimer?.Stop();
                _saveTimer?.Dispose();
            }
            catch { }

            try
            {
                _integrityCheckTimer?.Stop();
                _integrityCheckTimer?.Dispose();
            }
            catch { }

            SaveAllData();
        }
    }
}
