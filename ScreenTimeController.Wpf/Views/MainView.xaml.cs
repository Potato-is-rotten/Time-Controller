using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using ScreenTimeController.Wpf.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenTimeController.Wpf.Views;

public partial class MainView : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SettingsManager _settingsManager;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private bool _isVerifyingProtectionPassword;
    private bool _isClosing;

    public MainView()
    {
        InitializeComponent();
        _settingsManager = new SettingsManager();
        _viewModel = new MainViewModel(_settingsManager);
        DataContext = _viewModel;
        
        SetupNotifyIcon();
        SetupEventHandlers();
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Start();
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
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "AppIcon.ico"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppIcon.ico")
        };

        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                try
                {
                    icon = new Icon(path);
                    break;
                }
                catch { }
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
            _notifyIcon!.Icon = icon;
        }
    }

    private void SetupEventHandlers()
    {
        _viewModel.RequestSettings += OnViewModelRequestSettings;
        _viewModel.RequestExit += OnViewModelRequestExit;
        _viewModel.RequestShowWindow += OnViewModelRequestShowWindow;
        _viewModel.RequestUnlock += OnViewModelRequestUnlock;
        _viewModel.RequestProtectionAccess += OnViewModelRequestProtectionAccess;
        _viewModel.MinimizeToTray += OnViewModelMinimizeToTray;
    }

    private void OnViewModelRequestSettings(object? sender, EventArgs e)
    {
        var settingsView = new SettingsView(_settingsManager);
        settingsView.ShowDialog();
    }

    private void OnViewModelRequestExit(object? sender, EventArgs e)
    {
        if (_settingsManager.HasPassword())
        {
            var passwordView = new PasswordView(_settingsManager);
            if (passwordView.ShowDialog() != true || !passwordView.IsPasswordCorrect)
            {
                return;
            }
        }
        ExitApplication();
    }

    private void OnViewModelRequestShowWindow(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnViewModelRequestUnlock(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var unlockView = new UnlockView(_settingsManager, new TimeTracker(_settingsManager));
            if (unlockView.ShowDialog() == true && unlockView.IsPasswordCorrect)
            {
                _viewModel.OnUnlockSuccess();
            }
            else
            {
                try
                {
                    WindowHelper.LockWorkStation();
                }
                catch { }
            }
        });
    }

    private void OnViewModelRequestProtectionAccess(object? sender, EventArgs e)
    {
        if (_isVerifyingProtectionPassword) return;

        if (_settingsManager.HasPassword())
        {
            Dispatcher.BeginInvoke(new Action(ShowProtectionPasswordDialog));
        }
    }

    private void ShowProtectionPasswordDialog()
    {
        var passwordView = new PasswordView(_settingsManager);
        if (passwordView.ShowDialog() == true && passwordView.IsPasswordCorrect)
        {
            _isVerifyingProtectionPassword = true;
            try
            {
                _viewModel.SelectedTabIndex = 2;
                _viewModel.UpdateProtectionStatus();
            }
            finally
            {
                _isVerifyingProtectionPassword = false;
            }
        }
        else
        {
            _viewModel.SelectedTabIndex = 0;
        }
    }

    private void OnViewModelMinimizeToTray(object? sender, EventArgs e)
    {
        _notifyIcon?.ShowBalloonTip(2000, "Screen Time Controller", "Application minimized to tray.", ToolTipIcon.Info);
    }

    private void OnOpenClick(object? sender, EventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        var settingsView = new SettingsView(_settingsManager);
        settingsView.ShowDialog();
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        if (_settingsManager.HasPassword())
        {
            var passwordView = new PasswordView(_settingsManager);
            if (passwordView.ShowDialog() != true || !passwordView.IsPasswordCorrect)
            {
                return;
            }
        }
        ExitApplication();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _viewModel.OnWindowMinimized();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosing) return;

        e.Cancel = true;
        Hide();
        _notifyIcon?.ShowBalloonTip(2000, "Screen Time Controller", "Application minimized to tray. Click to open.", ToolTipIcon.Info);
    }

    private void ExitApplication()
    {
        _isClosing = true;
        _viewModel.Dispose();
        _notifyIcon!.Visible = false;
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
        Application.Current.Shutdown();
    }

    private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
}
