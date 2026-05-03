using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;

namespace ScreenTimeController;

public static class TaskSchedulerManager
{
    private const string TaskName = "ScreenTimeController_Restart";
    private const string TaskDescription = "Automatically restart Screen Time Controller when it exits unexpectedly";
    private const string ServiceName = "ScreenTimeController_ProtectionService";

    public static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static bool IsServiceInstalled()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT * FROM Win32_Service WHERE Name = '{ServiceName}'");
            foreach (System.Management.ManagementObject service in searcher.Get())
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsTaskInstalled()
    {
        try
        {
            using TaskService taskService = new();
            return taskService.FindTask(TaskName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static (bool success, string message) InstallTaskWithMessage()
    {
        try
        {
            string? mainExePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(mainExePath) || !mainExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                string? assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    mainExePath = Path.ChangeExtension(assemblyPath, ".exe");
                }
            }
            if (string.IsNullOrEmpty(mainExePath) || !File.Exists(mainExePath))
            {
                return (false, "Cannot determine executable path.");
            }

            string watchdogPath = Path.Combine(Path.GetDirectoryName(mainExePath)!, "WatchdogMonitor.exe");
            if (!File.Exists(watchdogPath))
            {
                return (false, "WatchdogMonitor.exe not found.");
            }

            using TaskService taskService = new();
            TaskDefinition taskDefinition = taskService.NewTask();
            taskDefinition.RegistrationInfo.Description = TaskDescription;
            taskDefinition.Settings.Enabled = true;
            taskDefinition.Settings.Hidden = true;
            taskDefinition.Settings.StartWhenAvailable = true;
            taskDefinition.Settings.DisallowStartIfOnBatteries = false;
            taskDefinition.Settings.StopIfGoingOnBatteries = false;
            taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            taskDefinition.Triggers.Add(new BootTrigger { Delay = TimeSpan.FromSeconds(30) });
            taskDefinition.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(10) });
            taskDefinition.Triggers.Add(new DailyTrigger { DaysInterval = 1, StartBoundary = DateTime.Today.AddMinutes(1) });

            ExecAction execAction = new(watchdogPath);
            taskDefinition.Actions.Add(execAction);

            taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
            return (true, "Task installed successfully.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Access denied. Please run as Administrator.");
        }
        catch (Exception ex)
        {
            LogError("InstallTask failed: " + ex.Message);
            return (false, $"Failed to install task: {ex.Message}");
        }
    }

    public static bool InstallTask()
    {
        return InstallTaskWithMessage().success;
    }

    public static (bool success, string message) UninstallTaskWithMessage()
    {
        try
        {
            using TaskService taskService = new();
            taskService.RootFolder.DeleteTask(TaskName, false);
            return (true, "Task uninstalled successfully.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, "Access denied. Please run as Administrator.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to uninstall task: {ex.Message}");
        }
    }

    public static bool EnsureTaskInstalled()
    {
        if (IsTaskInstalled())
        {
            return true;
        }
        return InstallTask();
    }

    public static bool RunTaskNow()
    {
        try
        {
            using TaskService taskService = new();
            Microsoft.Win32.TaskScheduler.Task? task = taskService.FindTask(TaskName);
            if (task == null)
            {
                return false;
            }
            task.Run();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void LogError(string message)
    {
        try
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            string logPath = Path.Combine(logDir, "task_scheduler.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
