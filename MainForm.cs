using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ScreenTimeController
{
    public partial class MainForm : Form
    {
        private readonly SettingsManager _settingsManager;
        private readonly TimeTracker _timeTracker;
        private System.Windows.Forms.Timer _uiTimer;
        private System.Windows.Forms.Timer _trackingTimer;
        private System.Windows.Forms.Timer _lockCheckTimer;
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private bool _isLocked;
        private bool _hasWarned5Minutes;
        private bool _isDisposed;
        private ImageList _appIconList;
        private ListView _listBoxAppUsage;
        private bool _isUpdating;
        private Dictionary<string, Icon> _iconCache;
        private Dictionary<string, string> _appFilePathCache;
        private TabControl _tabControl;
        private TabPage _tabOverview;
        private TabPage _tabApps;
        private Label _labelAppUsage;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public MainForm()
        {
            InitializeComponent();
            _settingsManager = new SettingsManager();
            _timeTracker = new TimeTracker(_settingsManager);
            _isLocked = false;
            _hasWarned5Minutes = false;
            _isDisposed = false;
            _isUpdating = false;
            _iconCache = new Dictionary<string, Icon>();
            _appFilePathCache = new Dictionary<string, string>();

            SetupNotifyIcon();
            SetupTimers();
            SetupIconList();
            ApplyLanguage();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateUI();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            this.Text = LanguageManager.GetString("AppTitle");
            _tabOverview.Text = LanguageManager.GetString("Overview");
            _tabApps.Text = LanguageManager.GetString("Applications");
            _buttonSettings.Text = LanguageManager.GetString("Settings");
            _labelAppUsage.Text = LanguageManager.GetString("ApplicationUsage");
            _contextMenu.Items[0].Text = LanguageManager.GetString("Open");
            _contextMenu.Items[1].Text = LanguageManager.GetString("Settings");
            _contextMenu.Items[3].Text = LanguageManager.GetString("Exit");
        }

        private void SetupIconList()
        {
            _appIconList = new ImageList
            {
                ImageSize = new Size(48, 48),
                ColorDepth = ColorDepth.Depth32Bit
            };
            _listBoxAppUsage.LargeImageList = _appIconList;
        }

        private void SetupNotifyIcon()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Open", null, OnOpenClick);
            _contextMenu.Items.Add("Settings", null, OnSettingsClick);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("Exit", null, OnExitClick);

            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "Screen Time Controller"
            };

            LoadAppIcon();
            _notifyIcon.DoubleClick += OnOpenClick;
        }

        private void LoadAppIcon()
        {
            Icon icon = null;
            string[] iconPaths = new string[]
            {
                Path.Combine(Application.StartupPath, "Resources", "AppIcon.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AppIcon.ico"),
                Path.Combine(Application.StartupPath, "AppIcon.ico"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppIcon.ico")
            };

            foreach (var path in iconPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        icon = new Icon(path);
                        break;
                    }
                    catch
                    {
                    }
                }
            }

            if (icon == null)
            {
                try
                {
                    icon = SystemIcons.Application;
                }
                catch
                {
                }
            }

            if (icon != null)
            {
                this.Icon = icon;
                _notifyIcon.Icon = icon;
            }
        }

        private void SetupTimers()
        {
            _uiTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            _uiTimer.Tick += OnUITimerTick;
            _uiTimer.Start();

            _trackingTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _trackingTimer.Tick += OnTrackingTimerTick;
            _trackingTimer.Start();

            _lockCheckTimer = new System.Windows.Forms.Timer
            {
                Interval = 10000
            };
            _lockCheckTimer.Tick += OnLockCheckTick;
            _lockCheckTimer.Start();
        }

        private void OnLockCheckTick(object sender, EventArgs e)
        {
            if (_isLocked)
            {
                _lockCheckTimer.Stop();
                ShowUnlockDialog();
                _lockCheckTimer.Start();
            }
        }

        private void ShowUnlockDialog()
        {
            using (var unlockForm = new UnlockForm(_settingsManager, _timeTracker))
            {
                var result = unlockForm.ShowDialog();
                if (result == DialogResult.OK && unlockForm.IsPasswordCorrect)
                {
                    _isLocked = false;
                    _hasWarned5Minutes = false;
                }
                else
                {
                    try
                    {
                        WindowHelper.LockWorkStation();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void OnUITimerTick(object sender, EventArgs e)
        {
            if (_isDisposed || _isUpdating) return;
            try
            {
                UpdateUI();
            }
            catch
            {
            }
        }

        private void OnTrackingTimerTick(object sender, EventArgs e)
        {
            if (_isDisposed) return;
            try
            {
                var appName = WindowHelper.GetActiveWindowProcessName();
                if (!string.IsNullOrEmpty(appName) && WindowHelper.ProcessHasWindow(appName))
                {
                    _timeTracker.RecordUsage(TimeSpan.FromSeconds(1), appName);
                    CacheAppIcon(appName);
                }

                CheckTimeLimit();
            }
            catch
            {
            }
        }

        private void CacheAppIcon(string processName)
        {
            if (string.IsNullOrEmpty(processName) || _iconCache.ContainsKey(processName)) return;

            Process[] processes = null;
            try
            {
                processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    try
                    {
                        var filePath = processes[0].MainModule?.FileName;
                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            _appFilePathCache[processName] = filePath;
                            var icon = Icon.ExtractAssociatedIcon(filePath);
                            if (icon != null)
                            {
                                _iconCache[processName] = icon;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var p in processes)
                    {
                        try
                        {
                            p.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void CheckTimeLimit()
        {
            try
            {
                var dailyLimit = _timeTracker.GetDailyLimit();
                var totalUsage = _timeTracker.TotalUsage;
                var bonusTime = _timeTracker.BonusTime;
                var effectiveLimit = dailyLimit + bonusTime;
                var remaining = effectiveLimit - totalUsage;

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
            catch
            {
            }
        }

        private void Show5MinuteWarning()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Show5MinuteWarning));
                return;
            }

            try
            {
                _notifyIcon.ShowBalloonTip(5000, LanguageManager.GetString("ScreenTimeWarning"), LanguageManager.GetString("FiveMinutesRemaining"), ToolTipIcon.Warning);
            }
            catch
            {
            }
        }

        private void LockScreen()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(LockScreen));
                return;
            }

            try
            {
                WindowHelper.LockWorkStation();
            }
            catch
            {
            }
        }

        private Icon GetAppIcon(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return null;

            if (_iconCache.ContainsKey(processName))
            {
                return _iconCache[processName];
            }

            if (_appFilePathCache.ContainsKey(processName))
            {
                try
                {
                    var filePath = _appFilePathCache[processName];
                    if (File.Exists(filePath))
                    {
                        var icon = Icon.ExtractAssociatedIcon(filePath);
                        if (icon != null)
                        {
                            _iconCache[processName] = icon;
                            return icon;
                        }
                    }
                }
                catch
                {
                }
            }

            CacheAppIcon(processName);
            return _iconCache.ContainsKey(processName) ? _iconCache[processName] : null;
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateUI));
                return;
            }

            if (_isUpdating || _isDisposed) return;
            _isUpdating = true;

            try
            {
                var dailyLimit = _timeTracker.GetDailyLimit();
                var totalUsage = _timeTracker.TotalUsage;
                var bonusTime = _timeTracker.BonusTime;
                var effectiveLimit = dailyLimit + bonusTime;
                var remaining = effectiveLimit - totalUsage;

                if (remaining < TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                }

                _labelDailyLimit.Text = $"{LanguageManager.GetString("DailyLimit")}: {dailyLimit.Hours}h {dailyLimit.Minutes}m";
                _labelUsedToday.Text = $"{LanguageManager.GetString("UsedToday")}: {totalUsage.Hours}h {totalUsage.Minutes}m";
                
                if (bonusTime > TimeSpan.Zero)
                {
                    _labelRemaining.Text = $"{LanguageManager.GetString("Remaining")}: {remaining.Hours}h {remaining.Minutes}m (+{bonusTime.Minutes}m bonus)";
                }
                else
                {
                    _labelRemaining.Text = $"{LanguageManager.GetString("Remaining")}: {remaining.Hours}h {remaining.Minutes}m";
                }

                var progress = 0;
                if (dailyLimit.TotalSeconds > 0)
                {
                    progress = (int)((totalUsage.TotalSeconds / dailyLimit.TotalSeconds) * 100);
                    if (progress > 100) progress = 100;
                }
                _progressBarUsage.Value = progress;

                var currentApps = _timeTracker.AppUsage;
                bool needRefresh = false;

                if (_listBoxAppUsage.Items.Count != currentApps.Count)
                {
                    needRefresh = true;
                }
                else
                {
                    int i = 0;
                    foreach (var app in currentApps)
                    {
                        if (i >= _listBoxAppUsage.Items.Count)
                        {
                            needRefresh = true;
                            break;
                        }
                        var item = _listBoxAppUsage.Items[i];
                        if (item.Text != app.Key)
                        {
                            needRefresh = true;
                            break;
                        }
                        i++;
                    }
                }

                if (needRefresh)
                {
                    _appIconList.Images.Clear();
                    _listBoxAppUsage.Items.Clear();

                    foreach (var app in currentApps)
                    {
                        var icon = GetAppIcon(app.Key);
                        if (icon != null)
                        {
                            _appIconList.Images.Add(app.Key, icon);
                        }

                        var item = new ListViewItem
                        {
                            Text = app.Key,
                            ImageKey = icon != null ? app.Key : "",
                            Font = new Font("Segoe UI", 12, FontStyle.Bold)
                        };
                        item.SubItems.Add($"{app.Value.Hours}h {app.Value.Minutes}m {app.Value.Seconds}s");
                        item.SubItems[1].Font = new Font("Segoe UI", 11, FontStyle.Regular);
                        item.SubItems[1].ForeColor = Color.FromArgb(100, 100, 100);

                        _listBoxAppUsage.Items.Add(item);
                    }
                }
                else
                {
                    int i = 0;
                    foreach (var app in currentApps)
                    {
                        if (i < _listBoxAppUsage.Items.Count)
                        {
                            _listBoxAppUsage.Items[i].SubItems[1].Text = $"{app.Value.Hours}h {app.Value.Minutes}m {app.Value.Seconds}s";
                        }
                        i++;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void OnOpenClick(object sender, EventArgs e)
        {
            try
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
            catch
            {
            }
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            try
            {
                using (var settingsForm = new SettingsForm(_settingsManager))
                {
                    settingsForm.ShowDialog();
                }
                _hasWarned5Minutes = false;
                UpdateUI();
            }
            catch
            {
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _isDisposed = true;
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            try
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    Hide();
                    _notifyIcon.ShowBalloonTip(2000, LanguageManager.GetString("AppTitle"), LanguageManager.GetString("MinimizedToTray"), ToolTipIcon.Info);
                }
            }
            catch
            {
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                try
                {
                    _notifyIcon.ShowBalloonTip(2000, LanguageManager.GetString("AppTitle"), LanguageManager.GetString("ClickToOpen"), ToolTipIcon.Info);
                }
                catch
                {
                }
            }
            else
            {
                CleanupResources();
            }
        }

        private void CleanupResources()
        {
            _isDisposed = true;
            LanguageManager.LanguageChanged -= OnLanguageChanged;

            try
            {
                _uiTimer?.Stop();
                _uiTimer?.Dispose();
            }
            catch { }

            try
            {
                _trackingTimer?.Stop();
                _trackingTimer?.Dispose();
            }
            catch { }

            try
            {
                _lockCheckTimer?.Stop();
                _lockCheckTimer?.Dispose();
            }
            catch { }

            try
            {
                _timeTracker?.Dispose();
            }
            catch { }

            try
            {
                if (_iconCache != null)
                {
                    foreach (var icon in _iconCache.Values)
                    {
                        try
                        {
                            icon?.Dispose();
                        }
                        catch { }
                    }
                    _iconCache.Clear();
                }
            }
            catch { }

            try
            {
                _appIconList?.Dispose();
            }
            catch { }

            try
            {
                _notifyIcon?.Dispose();
            }
            catch { }

            try
            {
                _contextMenu?.Dispose();
            }
            catch { }
        }

        private Label _labelDailyLimit;
        private Label _labelUsedToday;
        private Label _labelRemaining;
        private ProgressBar _progressBarUsage;
        private Button _buttonSettings;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 800);
            this.Text = "Screen Time Controller";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;

            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 12),
                Padding = new Point(20, 10)
            };

            _tabOverview = new TabPage("Overview")
            {
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(20)
            };

            _labelDailyLimit = new Label
            {
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(20, 30),
                Size = new Size(920, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _tabOverview.Controls.Add(_labelDailyLimit);

            _labelUsedToday = new Label
            {
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                Location = new Point(20, 110),
                Size = new Size(920, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _tabOverview.Controls.Add(_labelUsedToday);

            _labelRemaining = new Label
            {
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                Location = new Point(20, 190),
                Size = new Size(920, 70),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _tabOverview.Controls.Add(_labelRemaining);

            _progressBarUsage = new ProgressBar
            {
                Location = new Point(20, 280),
                Size = new Size(920, 60),
                Style = ProgressBarStyle.Continuous
            };
            _tabOverview.Controls.Add(_progressBarUsage);

            _buttonSettings = new Button
            {
                Font = new Font("Segoe UI", 14),
                Location = new Point(430, 680),
                Size = new Size(140, 50),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _buttonSettings.FlatAppearance.BorderSize = 0;
            _buttonSettings.Click += OnSettingsClick;
            _tabOverview.Controls.Add(_buttonSettings);

            _tabControl.TabPages.Add(_tabOverview);

            _tabApps = new TabPage("Applications")
            {
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(20)
            };

            _labelAppUsage = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(920, 50)
            };
            _tabApps.Controls.Add(_labelAppUsage);

            _listBoxAppUsage = new ListView
            {
                Location = new Point(20, 80),
                Size = new Size(920, 650),
                Font = new Font("Segoe UI", 12),
                View = View.Details,
                FullRowSelect = true,
                HeaderStyle = ColumnHeaderStyle.None
            };
            _listBoxAppUsage.Columns.Add("Application", 600);
            _listBoxAppUsage.Columns.Add("Time", 300);
            _tabApps.Controls.Add(_listBoxAppUsage);

            _tabControl.TabPages.Add(_tabApps);

            this.Controls.Add(_tabControl);
        }

        private System.ComponentModel.IContainer components;
    }
}
