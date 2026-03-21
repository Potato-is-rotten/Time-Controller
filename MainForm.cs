using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenTimeController;

public class MainForm : Form
{
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;
    private Timer? _uiTimer;
    private Timer? _trackingTimer;
    private Timer? _lockCheckTimer;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _isLocked;
    private bool _hasWarned5Minutes;
    private bool _isDisposed;
    private ImageList? _appIconList;
    private ListView? _listBoxAppUsage;
    private bool _isUpdating;
    private Dictionary<string, Icon> _iconCache;
    private Dictionary<string, string> _appFilePathCache;
    private Dictionary<string, string> _appFriendlyNames;
    private Font? _appItemFont;
    private Font? _appItemSubFont;
    private TabControl? _tabControl;
    private TabPage? _tabOverview;
    private TabPage? _tabApps;
    private TabPage? _tabProtection;
    private Label? _labelAppUsage;
    private Label? _labelDailyLimit;
    private Label? _labelUsedToday;
    private Label? _labelRemaining;
    private ProgressBar? _progressBarUsage;
    private Button? _buttonSettings;
    private Label? _labelAbnormalExits;
    private Label? _labelServiceStatus;
    private Label? _labelTaskStatus;
    private Label? _labelAbnormalTitle;
    private Label? _labelServiceTitle;
    private Label? _labelTaskTitle;
    private Label? _labelProtectionDesc;
    private Timer? _protectionStatusTimer;
    private IContainer? components;
    private bool _isVerifyingProtectionPassword;

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
        _isVerifyingProtectionPassword = false;
        _iconCache = new Dictionary<string, Icon>();
        _appFilePathCache = new Dictionary<string, string>();
        _appFriendlyNames = new Dictionary<string, string>();
        _appItemFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        _appItemSubFont = new Font("Segoe UI", 11f, FontStyle.Regular);
        SetupNotifyIcon();
        SetupTimers();
        SetupIconList();
        ApplyLanguage();
        LanguageManager.LanguageChanged += OnLanguageChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        UpdateUI();
        UpdateProtectionStatus();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (_isDisposed || !IsHandleCreated)
        {
            return;
        }
        try
        {
            BeginInvoke(new Action(AdjustWindowForDisplayChange));
        }
        catch { }
    }

    private void AdjustWindowForDisplayChange()
    {
        if (_isDisposed)
        {
            return;
        }
        try
        {
            Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
            int maxWidth = (int)(screenBounds.Width * 0.7);
            int maxHeight = (int)(screenBounds.Height * 0.7);
            int windowWidth = Math.Min(1100, maxWidth);
            int windowHeight = Math.Min(900, maxHeight);
            if (Width > screenBounds.Width || Height > screenBounds.Height)
            {
                ClientSize = new Size(windowWidth, windowHeight);
                CenterToScreen();
            }
        }
        catch { }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = LanguageManager.GetString("AppTitle");
        _tabOverview!.Text = LanguageManager.GetString("Overview");
        _tabApps!.Text = LanguageManager.GetString("Applications");
        _tabProtection!.Text = LanguageManager.GetString("Protection");
        _buttonSettings!.Text = LanguageManager.GetString("Settings");
        _labelAppUsage!.Text = LanguageManager.GetString("ApplicationUsage");
        _contextMenu!.Items[0].Text = LanguageManager.GetString("Open");
        _contextMenu.Items[1].Text = LanguageManager.GetString("Settings");
        _contextMenu.Items[3].Text = LanguageManager.GetString("Exit");
        _labelAbnormalTitle!.Text = LanguageManager.GetString("AbnormalExitsToday") + ":";
        _labelServiceTitle!.Text = LanguageManager.GetString("ServiceStatus") + ":";
        _labelTaskTitle!.Text = LanguageManager.GetString("TaskStatus") + ":";
        _labelProtectionDesc!.Text = LanguageManager.GetString("ProtectionDescription");
        UpdateProtectionStatus();
    }

    private void SetupIconList()
    {
        _appIconList = new ImageList
        {
            ImageSize = new Size(48, 48),
            ColorDepth = ColorDepth.Depth32Bit
        };
        _listBoxAppUsage!.LargeImageList = _appIconList;
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
        Icon? icon = null;
        string[] paths = new string[]
        {
            Path.Combine(Application.StartupPath, "Resources", "AppIcon.ico"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AppIcon.ico"),
            Path.Combine(Application.StartupPath, "AppIcon.ico"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppIcon.ico")
        };
        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                try
                {
                    icon = new Icon(path);
                }
                catch
                {
                    continue;
                }
                break;
            }
        }
        if (icon == null)
        {
            try
            {
                icon = SystemIcons.Application;
            }
            catch { }
        }
        if (icon != null)
        {
            Icon = icon;
            _notifyIcon!.Icon = icon;
        }
    }

    private void SetupTimers()
    {
        _uiTimer = new Timer { Interval = 10000 };
        _uiTimer.Tick += OnUITimerTick;
        _uiTimer.Start();

        _trackingTimer = new Timer { Interval = 2000 };
        _trackingTimer.Tick += OnTrackingTimerTick;
        _trackingTimer.Start();

        _lockCheckTimer = new Timer { Interval = 1000 };
        _lockCheckTimer.Tick += OnLockCheckTick;
        _lockCheckTimer.Start();
    }

    private void OnLockCheckTick(object? sender, EventArgs e)
    {
        if (_isLocked)
        {
            _lockCheckTimer!.Stop();
            ShowUnlockDialog();
            _lockCheckTimer.Start();
        }
    }

    private void ShowUnlockDialog()
    {
        using UnlockForm unlockForm = new(_settingsManager, _timeTracker);
        if (unlockForm.ShowDialog() == DialogResult.OK && unlockForm.IsPasswordCorrect)
        {
            _isLocked = false;
            _hasWarned5Minutes = false;
            return;
        }
        try
        {
            WindowHelper.LockWorkStation();
        }
        catch { }
    }

    private void OnUITimerTick(object? sender, EventArgs e)
    {
        if (_isDisposed || _isUpdating)
        {
            return;
        }
        try
        {
            UpdateUI();
        }
        catch { }
    }

    private void OnTrackingTimerTick(object? sender, EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }
        try
        {
            string? activeWindowProcessName = WindowHelper.GetActiveWindowProcessName();
            if (!string.IsNullOrEmpty(activeWindowProcessName))
            {
                string displayName = GetAppFriendlyName(activeWindowProcessName);
                _timeTracker.RecordUsage(TimeSpan.FromSeconds(2.0), displayName);
                if (!_iconCache.ContainsKey(activeWindowProcessName) && _iconCache.Count < 50)
                {
                    Task.Run(() => CacheAppIconAsync(activeWindowProcessName));
                }
            }
            CheckTimeLimit();
        }
        catch { }
    }

    private string GetAppFriendlyName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return "Unknown";
        }

        if (_appFriendlyNames.TryGetValue(processName, out string? friendlyName))
        {
            return friendlyName;
        }

        string name = processName;
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                try
                {
                    string? filePath = processes[0].MainModule?.FileName;
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        _appFilePathCache[processName] = filePath;
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                        if (!string.IsNullOrEmpty(versionInfo.FileDescription))
                        {
                            name = versionInfo.FileDescription;
                        }
                        else if (!string.IsNullOrEmpty(versionInfo.ProductName))
                        {
                            name = versionInfo.ProductName;
                        }
                    }
                }
                catch { }
                finally
                {
                    foreach (var p in processes)
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
            }
        }
        catch { }

        _appFriendlyNames[processName] = name;
        return name;
    }

    private async void CacheAppIconAsync(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return;

            try
            {
                string? filePath = processes[0].MainModule?.FileName;
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    _appFilePathCache[processName] = filePath;
                    Icon? icon = Icon.ExtractAssociatedIcon(filePath);
                    if (icon != null && !_iconCache.ContainsKey(processName))
                    {
                        _iconCache[processName] = icon;
                    }
                }
            }
            catch { }
            finally
            {
                foreach (var p in processes)
                {
                    try { p.Dispose(); } catch { }
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
            else if (remaining <= TimeSpan.FromMinutes(5.0) && remaining > TimeSpan.Zero && !_hasWarned5Minutes)
            {
                _hasWarned5Minutes = true;
                Show5MinuteWarning();
            }
        }
        catch { }
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
            _notifyIcon!.ShowBalloonTip(5000, LanguageManager.GetString("ScreenTimeWarning"), LanguageManager.GetString("FiveMinutesRemaining"), ToolTipIcon.Warning);
        }
        catch { }
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
        catch { }
    }

    private Icon? GetAppIcon(string processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return null;
        }
        if (_iconCache.TryGetValue(processName, out Icon? cachedIcon))
        {
            return cachedIcon;
        }
        if (_appFilePathCache.TryGetValue(processName, out string? filePath) && File.Exists(filePath))
        {
            try
            {
                Icon? icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                {
                    _iconCache[processName] = icon;
                    return icon;
                }
            }
            catch { }
        }
        return null;
    }

    private void UpdateUI()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(UpdateUI));
        }
        else
        {
            if (_isUpdating || _isDisposed)
            {
                return;
            }
            _isUpdating = true;
            try
            {
                TimeSpan dailyLimit = _timeTracker.GetDailyLimit();
                TimeSpan totalUsage = _timeTracker.TotalUsage;
                TimeSpan bonusTime = _timeTracker.BonusTime;
                TimeSpan remaining = dailyLimit + bonusTime - totalUsage;
                if (remaining < TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                }
                _labelDailyLimit!.Text = string.Format("{0}: {1}h {2}m", LanguageManager.GetString("DailyLimit"), dailyLimit.Hours, dailyLimit.Minutes);
                _labelUsedToday!.Text = string.Format("{0}: {1}h {2}m", LanguageManager.GetString("UsedToday"), totalUsage.Hours, totalUsage.Minutes);
                if (bonusTime > TimeSpan.Zero)
                {
                    _labelRemaining!.Text = string.Format("{0}: {1}h {2}m (+{3}m bonus)", LanguageManager.GetString("Remaining"), remaining.Hours, remaining.Minutes, bonusTime.Minutes);
                }
                else
                {
                    _labelRemaining!.Text = string.Format("{0}: {1}h {2}m", LanguageManager.GetString("Remaining"), remaining.Hours, remaining.Minutes);
                }
                int progress = 0;
                if (dailyLimit.TotalSeconds > 0.0)
                {
                    progress = (int)(totalUsage.TotalSeconds / dailyLimit.TotalSeconds * 100.0);
                    if (progress > 100)
                    {
                        progress = 100;
                    }
                }
                _progressBarUsage!.Value = progress;
                Dictionary<string, TimeSpan> appUsage = _timeTracker.AppUsage;
                bool needsRefresh = false;
                if (_listBoxAppUsage!.Items.Count != appUsage.Count)
                {
                    needsRefresh = true;
                }
                else
                {
                    int idx = 0;
                    foreach (KeyValuePair<string, TimeSpan> item in appUsage)
                    {
                        if (idx >= _listBoxAppUsage.Items.Count)
                        {
                            needsRefresh = true;
                            break;
                        }
                        if (_listBoxAppUsage.Items[idx].Text != item.Key)
                        {
                            needsRefresh = true;
                            break;
                        }
                        idx++;
                    }
                }
                if (needsRefresh)
                {
                    for (int i = 0; i < _appIconList!.Images.Count; i++)
                    {
                        if (_appIconList.Images[i] != null)
                        {
                            try { _appIconList.Images[i].Dispose(); } catch { }
                        }
                    }
                    _appIconList.Images.Clear();
                    _listBoxAppUsage.Items.Clear();
                    foreach (KeyValuePair<string, TimeSpan> item in appUsage)
                    {
                        Icon? appIcon = GetAppIcon(item.Key);
                        if (appIcon != null)
                        {
                            _appIconList.Images.Add(item.Key, appIcon);
                        }
                        ListViewItem listViewItem = new ListViewItem
                        {
                            Text = item.Key,
                            ImageKey = ((appIcon != null) ? item.Key : ""),
                            Font = _appItemFont!
                        };
                        listViewItem.SubItems.Add($"{item.Value.Hours}h {item.Value.Minutes}m {item.Value.Seconds}s");
                        listViewItem.SubItems[1].Font = _appItemSubFont!;
                        listViewItem.SubItems[1].ForeColor = Color.FromArgb(100, 100, 100);
                        _listBoxAppUsage.Items.Add(listViewItem);
                    }
                    return;
                }
                int num3 = 0;
                foreach (KeyValuePair<string, TimeSpan> item in appUsage)
                {
                    if (num3 < _listBoxAppUsage.Items.Count)
                    {
                        _listBoxAppUsage.Items[num3].SubItems[1].Text = $"{item.Value.Hours}h {item.Value.Minutes}m {item.Value.Seconds}s";
                    }
                    num3++;
                }
            }
            catch { }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void OnOpenClick(object? sender, EventArgs e)
    {
        try
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }
        catch { }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        try
        {
            using SettingsForm settingsForm = new(_settingsManager);
            settingsForm.ShowDialog();
            _hasWarned5Minutes = false;
            UpdateUI();
        }
        catch { }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        if (_settingsManager.HasPassword())
        {
            using PasswordForm passwordForm = new(_settingsManager);
            if (passwordForm.ShowDialog() != DialogResult.OK || !passwordForm.IsPasswordCorrect)
            {
                return;
            }
        }
        ExitApplication();
    }

    private void ExitApplication()
    {
        _isDisposed = true;

        try { _uiTimer?.Stop(); } catch { }
        try { _trackingTimer?.Stop(); } catch { }
        try { _lockCheckTimer?.Stop(); } catch { }
        try { _protectionStatusTimer?.Stop(); } catch { }

        try { _timeTracker?.MarkCleanExit(); } catch { }
        try { _timeTracker?.ForceSave(); } catch { }

        try { _uiTimer?.Dispose(); } catch { }
        try { _trackingTimer?.Dispose(); } catch { }
        try { _lockCheckTimer?.Dispose(); } catch { }
        try { _protectionStatusTimer?.Dispose(); } catch { }
        try { _timeTracker?.Dispose(); } catch { }
        try { _notifyIcon!.Visible = false; _notifyIcon?.Dispose(); } catch { }

        Application.Exit();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }
        try
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _notifyIcon!.ShowBalloonTip(2000, LanguageManager.GetString("AppTitle"), LanguageManager.GetString("MinimizedToTray"), ToolTipIcon.Info);
            }
        }
        catch { }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            try
            {
                _notifyIcon!.ShowBalloonTip(2000, LanguageManager.GetString("AppTitle"), LanguageManager.GetString("ClickToOpen"), ToolTipIcon.Info);
            }
            catch { }
            return;
        }
        CleanupResources();
    }

    private void OnTabControlSelecting(object? sender, TabControlCancelEventArgs e)
    {
        if (e.TabPage == _tabProtection && !_isVerifyingProtectionPassword)
        {
            if (_settingsManager.HasPassword())
            {
                e.Cancel = true;
                BeginInvoke(new Action(ShowProtectionPasswordDialog));
            }
        }
    }

    private void ShowProtectionPasswordDialog()
    {
        using PasswordForm passwordForm = new(_settingsManager);
        if (passwordForm.ShowDialog() == DialogResult.OK && passwordForm.IsPasswordCorrect)
        {
            _isVerifyingProtectionPassword = true;
            try
            {
                _tabControl!.SelectedTab = _tabProtection;
                UpdateProtectionStatus();
            }
            finally
            {
                _isVerifyingProtectionPassword = false;
            }
        }
    }

    private void UpdateProtectionStatus()
    {
        if (InvokeRequired)
        {
            try { Invoke(new Action(UpdateProtectionStatus)); } catch { }
            return;
        }

        try
        {
            int abnormalCount = AbnormalExitTracker.GetTodayAbnormalExitCount();
            _labelAbnormalExits!.Text = string.Format(LanguageManager.GetString("AbnormalExitCount"), abnormalCount);
        }
        catch
        {
            _labelAbnormalExits!.Text = LanguageManager.GetString("AbnormalExitCount").Replace("{0}", "0");
        }

        try
        {
            bool taskInstalled = TaskSchedulerManager.IsTaskInstalled();
            _labelTaskStatus!.Text = taskInstalled
                ? LanguageManager.GetString("TaskInstalled")
                : LanguageManager.GetString("TaskNotInstalled");
            _labelTaskStatus.ForeColor = taskInstalled ? Color.FromArgb(0, 150, 0) : Color.FromArgb(200, 0, 0);
        }
        catch
        {
            _labelTaskStatus!.Text = LanguageManager.GetString("TaskNotInstalled");
            _labelTaskStatus.ForeColor = Color.FromArgb(200, 0, 0);
        }

        try
        {
            bool serviceRunning = IsServiceRunning();
            _labelServiceStatus!.Text = serviceRunning
                ? LanguageManager.GetString("ServiceRunning")
                : LanguageManager.GetString("ServiceStopped");
            _labelServiceStatus.ForeColor = serviceRunning ? Color.FromArgb(0, 150, 0) : Color.FromArgb(200, 0, 0);
        }
        catch
        {
            _labelServiceStatus!.Text = LanguageManager.GetString("ServiceStopped");
            _labelServiceStatus.ForeColor = Color.FromArgb(200, 0, 0);
        }
    }

    private static bool IsServiceRunning()
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

    private void OnProtectionTimerTick(object? sender, EventArgs e)
    {
        UpdateProtectionStatus();
    }

    private void ListBoxAppUsage_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), e.Bounds);
        using Font headerFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using StringFormat sf = new StringFormat()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        e.Graphics.DrawString(e.Header?.Text ?? "", headerFont, Brushes.Black, e.Bounds, sf);
    }

    private void CleanupResources()
    {
        _isDisposed = true;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch { }
        try { _uiTimer?.Stop(); _uiTimer?.Dispose(); } catch { }
        try { _trackingTimer?.Stop(); _trackingTimer?.Dispose(); } catch { }
        try { _lockCheckTimer?.Stop(); _lockCheckTimer?.Dispose(); } catch { }
        try { _timeTracker?.Dispose(); } catch { }
        try
        {
            if (_iconCache != null)
            {
                foreach (Icon value in _iconCache.Values)
                {
                    try { value?.Dispose(); } catch { }
                }
                _iconCache.Clear();
            }
        }
        catch { }
        try
        {
            for (int i = 0; i < _appIconList!.Images.Count; i++)
            {
                if (_appIconList.Images[i] != null)
                {
                    try { _appIconList.Images[i].Dispose(); } catch { }
                }
            }
            _appIconList?.Dispose();
        }
        catch { }
        try { _notifyIcon?.Dispose(); } catch { }
        try { _contextMenu?.Dispose(); } catch { }
        try { _protectionStatusTimer?.Stop(); _protectionStatusTimer?.Dispose(); } catch { }
        try { _appItemFont?.Dispose(); } catch { }
        try { _appItemSubFont?.Dispose(); } catch { }
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
        int maxWidth = (int)(screenBounds.Width * 0.7);
        int maxHeight = (int)(screenBounds.Height * 0.7);
        int windowWidth = Math.Min(1100, maxWidth);
        int windowHeight = Math.Min(900, maxHeight);
        ClientSize = new Size(windowWidth, windowHeight);
        MinimumSize = new Size(900, 700);
        Text = "Screen Time Controller";
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 240, 240);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;
        Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        Resize += new EventHandler(MainForm_Resize);
        FormClosing += new FormClosingEventHandler(MainForm_FormClosing);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            Padding = new Point(40, 8),
            ItemSize = new Size(0, 48),
            SizeMode = TabSizeMode.FillToRight,
            Appearance = TabAppearance.Normal,
            Alignment = TabAlignment.Top
        };
        _tabControl.Selecting += OnTabControlSelecting;

        _tabOverview = new TabPage(LanguageManager.GetString("Overview"))
        {
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(20)
        };
        TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20)
        };
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
        tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _labelDailyLimit = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };
        tableLayoutPanel.Controls.Add(_labelDailyLimit, 0, 0);

        _labelUsedToday = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };
        tableLayoutPanel.Controls.Add(_labelUsedToday, 0, 1);

        _labelRemaining = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 122, 204),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };
        tableLayoutPanel.Controls.Add(_labelRemaining, 0, 2);

        _progressBarUsage = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Height = 60,
            Style = ProgressBarStyle.Continuous
        };
        tableLayoutPanel.Controls.Add(_progressBarUsage, 0, 3);

        Panel buttonPanel = new Panel { Dock = DockStyle.Fill };
        _buttonSettings = new Button
        {
            Font = new Font("Segoe UI", 14f),
            Size = new Size(160, 55),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.None
        };
        _buttonSettings.FlatAppearance.BorderSize = 0;
        _buttonSettings.Click += new EventHandler(OnSettingsClick);
        buttonPanel.Controls.Add(_buttonSettings);
        tableLayoutPanel.Controls.Add(buttonPanel, 0, 4);
        _tabOverview.Controls.Add(tableLayoutPanel);
        _tabControl.TabPages.Add(_tabOverview);

        _tabApps = new TabPage(LanguageManager.GetString("Applications"))
        {
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(20)
        };
        TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(15)
        };
        tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
        tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _labelAppUsage = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        tableLayoutPanel2.Controls.Add(_labelAppUsage, 0, 0);

        _listBoxAppUsage = new ListView
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            GridLines = false,
            OwnerDraw = true
        };
        _listBoxAppUsage.Columns.Add("Application", -2, HorizontalAlignment.Left);
        _listBoxAppUsage.Columns.Add("Time", -2, HorizontalAlignment.Left);
        _listBoxAppUsage.DrawColumnHeader += ListBoxAppUsage_DrawColumnHeader;
        _listBoxAppUsage.DrawItem += (s, e) => { };
        _listBoxAppUsage.DrawSubItem += (s, e) => e.DrawDefault = true;
        tableLayoutPanel2.Controls.Add(_listBoxAppUsage, 0, 1);
        _tabApps.Controls.Add(tableLayoutPanel2);
        _tabControl.TabPages.Add(_tabApps);

        _tabProtection = new TabPage(LanguageManager.GetString("Protection"))
        {
            BackColor = Color.FromArgb(240, 240, 240),
            Padding = new Padding(20)
        };
        TableLayoutPanel protectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(20)
        };
        protectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        protectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        protectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        protectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        protectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
        protectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
        protectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _labelAbnormalTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelAbnormalTitle, 0, 0);
        _labelAbnormalExits = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelAbnormalExits, 1, 0);

        _labelServiceTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelServiceTitle, 0, 1);
        _labelServiceStatus = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelServiceStatus, 1, 1);

        _labelTaskTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelTaskTitle, 0, 2);
        _labelTaskStatus = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 15, 0, 15),
            AutoEllipsis = true
        };
        protectionLayout.Controls.Add(_labelTaskStatus, 1, 2);

        Panel emptyPanel = new Panel { Dock = DockStyle.Fill };
        protectionLayout.Controls.Add(emptyPanel, 0, 3);

        _labelProtectionDesc = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(100, 100, 100),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        protectionLayout.SetColumnSpan(_labelProtectionDesc, 2);
        protectionLayout.Controls.Add(_labelProtectionDesc, 0, 3);

        _tabProtection.Controls.Add(protectionLayout);
        _tabControl.TabPages.Add(_tabProtection);

        _protectionStatusTimer = new Timer { Interval = 10000 };
        _protectionStatusTimer.Tick += OnProtectionTimerTick;
        _protectionStatusTimer.Start();

        Controls.Add(_tabControl);
        Load += delegate
        {
            _buttonSettings.Location = new Point((buttonPanel.Width - _buttonSettings.Width) / 2, (buttonPanel.Height - _buttonSettings.Height) / 2);
        };
        buttonPanel.Resize += delegate
        {
            _buttonSettings.Location = new Point((buttonPanel.Width - _buttonSettings.Width) / 2, (buttonPanel.Height - _buttonSettings.Height) / 2);
        };
    }
}
