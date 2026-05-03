using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace ScreenTimeController.Wpf.ViewModels;

public class InstallWizardViewModel : ViewModelBase
{
    private bool _installProtectionService = true;
    private bool _installWatchdog = true;
    private bool _installScheduledTask = true;
    private bool _isInstalling;
    private string _statusMessage = "";
    private double _progress;
    private bool _installCompleted;

    public bool InstallProtectionService
    {
        get => _installProtectionService;
        set => SetProperty(ref _installProtectionService, value);
    }

    public bool InstallWatchdog
    {
        get => _installWatchdog;
        set => SetProperty(ref _installWatchdog, value);
    }

    public bool InstallScheduledTask
    {
        get => _installScheduledTask;
        set => SetProperty(ref _installScheduledTask, value);
    }

    public bool IsInstalling
    {
        get => _isInstalling;
        set
        {
            if (SetProperty(ref _isInstalling, value))
            {
                OnPropertyChanged(nameof(CanInteract));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public bool InstallCompleted
    {
        get => _installCompleted;
        set => SetProperty(ref _installCompleted, value);
    }

    public bool CanInteract => !IsInstalling;

    public ICommand InstallCommand { get; }
    public ICommand SkipCommand { get; }

    public event EventHandler? InstallFinished;
    public event EventHandler? SkipRequested;

    public InstallWizardViewModel()
    {
        InstallCommand = new RelayCommand(ExecuteInstall, CanExecuteInstall);
        SkipCommand = new RelayCommand(ExecuteSkip, CanExecuteSkip);
        CheckPreconditions();
    }

    private void CheckPreconditions()
    {
        bool serviceInstalled = TaskSchedulerManager.IsServiceInstalled();
        bool taskInstalled = TaskSchedulerManager.IsTaskInstalled();

        if (serviceInstalled)
        {
            InstallProtectionService = false;
        }

        if (taskInstalled)
        {
            InstallScheduledTask = false;
        }
    }

    private bool CanExecuteInstall(object? parameter)
    {
        return !IsInstalling && (InstallProtectionService || InstallWatchdog || InstallScheduledTask);
    }

    private bool CanExecuteSkip(object? parameter)
    {
        return !IsInstalling;
    }

    private async void ExecuteInstall(object? parameter)
    {
        IsInstalling = true;
        StatusMessage = LanguageManager.GetString("InstallingProtectionService");
        Progress = 0;

        try
        {
            if (InstallProtectionService)
            {
                await InstallProtectionServiceAsync();
                Progress = 33;
            }

            if (InstallWatchdog)
            {
                StatusMessage = LanguageManager.GetString("InstallingWatchdog");
                await InstallWatchdogAsync();
                Progress = 66;
            }

            if (InstallScheduledTask)
            {
                StatusMessage = LanguageManager.GetString("InstallingScheduledTask");
                await InstallScheduledTaskAsync();
                Progress = 100;
            }

            StatusMessage = LanguageManager.GetString("InstallationComplete");
            InstallCompleted = true;
            InstallFinished?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"{LanguageManager.GetString("InstallationFailed")}: {ex.Message}";
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void ExecuteSkip(object? parameter)
    {
        SkipRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task InstallProtectionServiceAsync()
    {
        await Task.Run(() =>
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProtectionService.exe");
            string scriptPath = Path.Combine(Path.GetDirectoryName(exePath)!, "install_service.bat");

            string script = $@"@echo off
sc create ""ScreenTimeController_ProtectionService"" binPath= ""{exePath}"" start= delayed-auto
sc description ""ScreenTimeController_ProtectionService"" ""Screen time controller protection service""
net start ""ScreenTimeController_ProtectionService""
";

            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process?.WaitForExit();

            try { File.Delete(scriptPath); } catch { }
        });
    }

    private async Task InstallWatchdogAsync()
    {
        await Task.Run(() =>
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WatchdogMonitor.exe");
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--install",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process?.WaitForExit();
        });
    }

    private async Task InstallScheduledTaskAsync()
    {
        await Task.Run(() =>
        {
            string watchdogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WatchdogMonitor.exe");
            string scriptPath = Path.Combine(Path.GetDirectoryName(watchdogPath)!, "install_task.bat");

            string script = $@"@echo off
schtasks /create /tn ""ScreenTimeController_Startup"" /tr ""{watchdogPath}"" /sc onlogon /rl limited
";

            File.WriteAllText(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process?.WaitForExit();

            try { File.Delete(scriptPath); } catch { }
        });
    }
}
