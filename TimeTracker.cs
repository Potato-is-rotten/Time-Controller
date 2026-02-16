using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Timers;

namespace ScreenTimeController
{
    public class TimeTracker : IDisposable
    {
        private TimeSpan _totalUsage;
        private TimeSpan _bonusTime;
        private readonly Dictionary<string, TimeSpan> _appUsage;
        private readonly SettingsManager _settingsManager;
        private readonly string _usageFilePath;
        private readonly string _appUsageFilePath;
        private System.Timers.Timer _midnightTimer;
        private DateTime _lastCheckedDate;
        private readonly object _lockObject = new object();
        private bool _isDisposed;

        public TimeSpan TotalUsage
        {
            get
            {
                lock (_lockObject)
                {
                    return _totalUsage;
                }
            }
        }

        public TimeSpan BonusTime
        {
            get
            {
                lock (_lockObject)
                {
                    return _bonusTime;
                }
            }
        }

        public TimeSpan EffectiveUsage
        {
            get
            {
                lock (_lockObject)
                {
                    return _totalUsage - _bonusTime;
                }
            }
        }

        public Dictionary<string, TimeSpan> AppUsage
        {
            get
            {
                lock (_lockObject)
                {
                    return new Dictionary<string, TimeSpan>(_appUsage);
                }
            }
        }

        public TimeTracker(SettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            _appUsage = new Dictionary<string, TimeSpan>();
            _bonusTime = TimeSpan.Zero;
            _usageFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTimeController", "usage.txt");
            _appUsageFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTimeController", "app_usage.txt");
            _lastCheckedDate = DateTime.Today;
            _isDisposed = false;
            InitializeFiles();
            LoadUsage();
            SetupMidnightTimer();
        }

        private void SetupMidnightTimer()
        {
            _midnightTimer = new System.Timers.Timer(60000);
            _midnightTimer.Elapsed += OnMidnightCheck;
            _midnightTimer.Start();
        }

        private void OnMidnightCheck(object sender, ElapsedEventArgs e)
        {
            CheckForNewDay();
        }

        private void CheckForNewDay()
        {
            DateTime currentDate = DateTime.Today;
            if (currentDate != _lastCheckedDate)
            {
                lock (_lockObject)
                {
                    _totalUsage = TimeSpan.Zero;
                    _bonusTime = TimeSpan.Zero;
                    _appUsage.Clear();
                    _lastCheckedDate = currentDate;
                    SaveUsageInternal();
                }
            }
        }

        private void InitializeFiles()
        {
            try
            {
                var directory = Path.GetDirectoryName(_usageFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_usageFilePath))
                {
                    File.WriteAllText(_usageFilePath, $"{DateTime.Today:yyyy-MM-dd}|0|0");
                }

                if (!File.Exists(_appUsageFilePath))
                {
                    File.WriteAllText(_appUsageFilePath, $"{DateTime.Today:yyyy-MM-dd}");
                }
            }
            catch
            {
            }
        }

        private void LoadUsage()
        {
            try
            {
                if (File.Exists(_usageFilePath))
                {
                    var content = File.ReadAllText(_usageFilePath);
                    var parts = content.Split('|');
                    if (parts.Length >= 2)
                    {
                        var date = DateTime.ParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        if (date.Date == DateTime.Today)
                        {
                            lock (_lockObject)
                            {
                                if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double minutes))
                                {
                                    _totalUsage = TimeSpan.FromMinutes(minutes);
                                }
                                if (parts.Length >= 3 && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double bonusMinutes))
                                {
                                    _bonusTime = TimeSpan.FromMinutes(bonusMinutes);
                                }
                            }
                        }
                        else
                        {
                            lock (_lockObject)
                            {
                                _totalUsage = TimeSpan.Zero;
                                _bonusTime = TimeSpan.Zero;
                                _appUsage.Clear();
                            }
                            SaveUsage();
                        }
                    }
                }

                if (File.Exists(_appUsageFilePath))
                {
                    var appContent = File.ReadAllText(_appUsageFilePath);
                    var appLines = appContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    if (appLines.Length > 0)
                    {
                        var appDateLine = appLines[0];
                        if (appDateLine.StartsWith(DateTime.Today.ToString("yyyy-MM-dd")))
                        {
                            lock (_lockObject)
                            {
                                for (int i = 1; i < appLines.Length; i++)
                                {
                                    var appParts = appLines[i].Split('|');
                                    if (appParts.Length == 2)
                                    {
                                        var appName = appParts[0];
                                        if (double.TryParse(appParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double appMinutes))
                                        {
                                            _appUsage[appName] = TimeSpan.FromMinutes(appMinutes);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            lock (_lockObject)
                            {
                                _appUsage.Clear();
                            }
                            SaveUsage();
                        }
                    }
                }
            }
            catch
            {
                lock (_lockObject)
                {
                    _totalUsage = TimeSpan.Zero;
                    _bonusTime = TimeSpan.Zero;
                    _appUsage.Clear();
                }
                SaveUsage();
            }
        }

        public void RecordUsage(TimeSpan duration, string appName = "Unknown")
        {
            if (duration <= TimeSpan.Zero || string.IsNullOrEmpty(appName))
                return;

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
            SaveUsage();
        }

        public void AddBonusTime(TimeSpan bonus)
        {
            if (bonus <= TimeSpan.Zero)
                return;

            lock (_lockObject)
            {
                _bonusTime += bonus;
            }
            SaveUsage();
        }

        public void UseBonusTime(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                return;

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
            SaveUsage();
        }

        private void SaveUsage()
        {
            try
            {
                SaveUsageInternal();
            }
            catch
            {
            }
        }

        private void SaveUsageInternal()
        {
            try
            {
                var directory = Path.GetDirectoryName(_usageFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string usageContent;
                string appContent;
                
                lock (_lockObject)
                {
                    usageContent = $"{DateTime.Today:yyyy-MM-dd}|{_totalUsage.TotalMinutes.ToString(CultureInfo.InvariantCulture)}|{_bonusTime.TotalMinutes.ToString(CultureInfo.InvariantCulture)}";

                    appContent = $"{DateTime.Today:yyyy-MM-dd}" + Environment.NewLine;
                    foreach (var app in _appUsage)
                    {
                        appContent += $"{app.Key}|{app.Value.TotalMinutes.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine;
                    }
                }
                
                File.WriteAllText(_usageFilePath, usageContent);
                File.WriteAllText(_appUsageFilePath, appContent);
            }
            catch
            {
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _totalUsage = TimeSpan.Zero;
                _bonusTime = TimeSpan.Zero;
                _appUsage.Clear();
                SaveUsageInternal();
            }
        }

        public TimeSpan GetDailyLimit()
        {
            return _settingsManager.GetDailyLimit();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            _midnightTimer?.Stop();
            _midnightTimer?.Dispose();
        }
    }
}
