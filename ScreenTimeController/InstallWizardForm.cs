using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenTimeController;

public class InstallWizardForm : Form
{
    private Panel _centerPanel = null!;
    private Label _labelTitle = null!;
    private Label _labelDescription = null!;
    private CheckBox _checkBoxProtectionService = null!;
    private CheckBox _checkBoxScheduledTask = null!;
    private CheckBox _checkBoxWatchdog = null!;
    private Button _buttonInstall = null!;
    private Button _buttonSkip = null!;
    private Label _labelStatus = null!;
    private ProgressBar _progressBar = null!;

    private readonly string _protectionServicePath;
    private readonly string _watchdogPath;

    public bool InstallCompleted { get; private set; }

    public InstallWizardForm()
    {
        string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        _protectionServicePath = Path.Combine(currentDir ?? "", "ProtectionService.exe");
        _watchdogPath = Path.Combine(currentDir ?? "", "WatchdogMonitor.exe");

        InitializeComponent();
        CheckPreconditions();
    }

    private void CheckPreconditions()
    {
        bool serviceInstalled = TaskSchedulerManager.IsServiceInstalled();
        bool taskInstalled = TaskSchedulerManager.IsTaskInstalled();

        _checkBoxProtectionService.Enabled = File.Exists(_protectionServicePath);
        _checkBoxWatchdog.Enabled = File.Exists(_watchdogPath);

        if (serviceInstalled)
        {
            _checkBoxProtectionService.Checked = false;
            _checkBoxProtectionService.Text = LanguageManager.GetString("ProtectionService") + " (" + LanguageManager.GetString("AlreadyInstalled") + ")";
        }
        else if (!_checkBoxProtectionService.Enabled)
        {
            _checkBoxProtectionService.Checked = false;
            _checkBoxProtectionService.Text = LanguageManager.GetString("ProtectionService") + " (" + LanguageManager.GetString("FileNotFound") + ")";
        }

        if (taskInstalled)
        {
            _checkBoxScheduledTask.Checked = false;
            _checkBoxScheduledTask.Text = LanguageManager.GetString("ScheduledTask") + " (" + LanguageManager.GetString("AlreadyInstalled") + ")";
        }

        if (!_checkBoxWatchdog.Enabled)
        {
            _checkBoxWatchdog.Checked = false;
            _checkBoxWatchdog.Text = LanguageManager.GetString("WatchdogProcess") + " (" + LanguageManager.GetString("FileNotFound") + ")";
        }
    }

    private void InitializeComponent()
    {
        Text = LanguageManager.GetString("AppTitle");
        Size = new Size(800, 650);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);

        _centerPanel = new Panel
        {
            Size = new Size(760, 590),
            BackColor = Color.FromArgb(50, 50, 50),
            Location = new Point((Width - 760) / 2, 20)
        };

        _labelTitle = new Label
        {
            Text = LanguageManager.GetString("InstallationWizard"),
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 20),
            Size = new Size(760, 50)
        };
        _centerPanel.Controls.Add(_labelTitle);

        _labelDescription = new Label
        {
            Text = LanguageManager.GetString("InstallationWizardDescription"),
            Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(200, 200, 200),
            TextAlign = ContentAlignment.TopLeft,
            Location = new Point(30, 80),
            Size = new Size(700, 70),
            AutoSize = false
        };
        _centerPanel.Controls.Add(_labelDescription);

        int yPos = 170;

        _checkBoxProtectionService = new CheckBox
        {
            Text = LanguageManager.GetString("ProtectionService"),
            Font = new Font("Segoe UI", 13f),
            ForeColor = Color.White,
            Location = new Point(30, yPos),
            Size = new Size(700, 40),
            Checked = true
        };
        _centerPanel.Controls.Add(_checkBoxProtectionService);

        Label labelProtectionServiceDesc = new Label
        {
            Text = LanguageManager.GetString("ProtectionServiceDescription"),
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(50, yPos + 40),
            Size = new Size(680, 35),
            AutoSize = false
        };
        _centerPanel.Controls.Add(labelProtectionServiceDesc);

        yPos += 90;

        _checkBoxWatchdog = new CheckBox
        {
            Text = LanguageManager.GetString("WatchdogProcess"),
            Font = new Font("Segoe UI", 13f),
            ForeColor = Color.White,
            Location = new Point(30, yPos),
            Size = new Size(700, 40),
            Checked = true
        };
        _centerPanel.Controls.Add(_checkBoxWatchdog);

        Label labelWatchdogDesc = new Label
        {
            Text = LanguageManager.GetString("WatchdogProcessDescription"),
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(50, yPos + 40),
            Size = new Size(680, 35),
            AutoSize = false
        };
        _centerPanel.Controls.Add(labelWatchdogDesc);

        yPos += 90;

        _checkBoxScheduledTask = new CheckBox
        {
            Text = LanguageManager.GetString("ScheduledTask"),
            Font = new Font("Segoe UI", 13f),
            ForeColor = Color.White,
            Location = new Point(30, yPos),
            Size = new Size(700, 40),
            Checked = true
        };
        _centerPanel.Controls.Add(_checkBoxScheduledTask);

        Label labelScheduledTaskDesc = new Label
        {
            Text = LanguageManager.GetString("ScheduledTaskDescription"),
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(50, yPos + 40),
            Size = new Size(680, 35),
            AutoSize = false
        };
        _centerPanel.Controls.Add(labelScheduledTaskDesc);

        yPos += 95;

        _progressBar = new ProgressBar
        {
            Location = new Point(30, yPos),
            Size = new Size(700, 25),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };
        _centerPanel.Controls.Add(_progressBar);

        _labelStatus = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(100, 255, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, yPos + 35),
            Size = new Size(760, 30),
            Visible = false
        };
        _centerPanel.Controls.Add(_labelStatus);

        yPos += 85;

        _buttonInstall = new Button
        {
            Text = LanguageManager.GetString("Install"),
            Font = new Font("Segoe UI", 13f),
            Size = new Size(160, 50),
            Location = new Point(200, yPos),
            BackColor = Color.FromArgb(0, 122, 204),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "InstallButton"
        };
        _buttonInstall.FlatAppearance.BorderSize = 0;
        _buttonInstall.Click += OnInstallClick;
        _centerPanel.Controls.Add(_buttonInstall);

        _buttonSkip = new Button
        {
            Text = LanguageManager.GetString("Skip"),
            Font = new Font("Segoe UI", 13f),
            Size = new Size(160, 50),
            Location = new Point(400, yPos),
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AccessibleName = "SkipButton"
        };
        _buttonSkip.FlatAppearance.BorderSize = 0;
        _buttonSkip.Click += OnSkipClick;
        _centerPanel.Controls.Add(_buttonSkip);

        Controls.Add(_centerPanel);
    }

    private async void OnInstallClick(object? sender, EventArgs e)
    {
        SetUIInstalling(true);

        try
        {
            if (_checkBoxProtectionService.Checked)
            {
                _labelStatus.Text = LanguageManager.GetString("InstallingProtectionService");
                _labelStatus.Visible = true;
                await InstallProtectionServiceAsync();
            }

            if (_checkBoxWatchdog.Checked)
            {
                _labelStatus.Text = LanguageManager.GetString("InstallingWatchdog");
                await InstallWatchdogAsync();
            }

            if (_checkBoxScheduledTask.Checked)
            {
                _labelStatus.Text = LanguageManager.GetString("InstallingScheduledTask");
                await InstallScheduledTaskAsync();
            }

            _labelStatus.Text = LanguageManager.GetString("InstallationComplete");
            _labelStatus.ForeColor = Color.FromArgb(100, 255, 100);
            InstallCompleted = true;

            await Task.Delay(1500);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _labelStatus.Text = $"{LanguageManager.GetString("InstallationFailed")}: {ex.Message}";
            _labelStatus.ForeColor = Color.FromArgb(255, 100, 100);
            SetUIInstalling(false);
        }
    }

    private void OnSkipClick(object? sender, EventArgs e)
    {
        InstallCompleted = false;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void SetUIInstalling(bool installing)
    {
        _buttonInstall.Enabled = !installing;
        _buttonSkip.Enabled = !installing;
        _checkBoxProtectionService.Enabled = !installing;
        _checkBoxWatchdog.Enabled = !installing;
        _checkBoxScheduledTask.Enabled = !installing;
        _progressBar.Visible = installing;
    }

    private async Task InstallProtectionServiceAsync()
    {
        string exePath = _protectionServicePath;
        string scriptPath = Path.Combine(Path.GetDirectoryName(exePath) ?? "", "install_service.bat");

        string script = $@"@echo off
sc create ""ScreenTimeController_ProtectionService"" binPath= ""{exePath}"" start= delayed-auto
sc description ""ScreenTimeController_ProtectionService"" ""Screen time controller protection service""
net start ""ScreenTimeController_ProtectionService""
";

        await File.WriteAllTextAsync(scriptPath, script);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        Process? process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        try { File.Delete(scriptPath); } catch { }
    }

    private async Task InstallWatchdogAsync()
    {
        string exePath = _watchdogPath;
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--install",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        Process? process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    private async Task InstallScheduledTaskAsync()
    {
        string watchdogPath = _watchdogPath;
        string scriptPath = Path.Combine(Path.GetDirectoryName(watchdogPath) ?? "", "install_task.bat");

        string script = $@"@echo off
schtasks /create /tn ""ScreenTimeController_Startup"" /tr ""{watchdogPath}"" /sc onlogon /rl limited
";

        await File.WriteAllTextAsync(scriptPath, script);

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{scriptPath}\"",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true
        };

        Process? process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }

        try { File.Delete(scriptPath); } catch { }
    }
}
