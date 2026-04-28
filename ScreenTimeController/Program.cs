using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScreenTimeController;

internal static class Program
{
    private static Mutex? _mutex;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--install-task"))
        {
            if (!TaskSchedulerManager.IsAdministrator())
            {
                RestartAsAdmin(args);
                return;
            }
            AllocConsole();
            var (success, message) = TaskSchedulerManager.InstallTaskWithMessage();
            Console.WriteLine(message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            FreeConsole();
            return;
        }
        if (args.Contains("--uninstall-task"))
        {
            if (!TaskSchedulerManager.IsAdministrator())
            {
                RestartAsAdmin(args);
                return;
            }
            AllocConsole();
            var (success, message) = TaskSchedulerManager.UninstallTaskWithMessage();
            Console.WriteLine(message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            FreeConsole();
            return;
        }
        if (args.Contains("--check-task"))
        {
            AllocConsole();
            bool installed = TaskSchedulerManager.IsTaskInstalled();
            Console.WriteLine(installed ? "Task is installed." : "Task is NOT installed.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            FreeConsole();
            return;
        }
        if (args.Contains("--run-task"))
        {
            AllocConsole();
            bool success = TaskSchedulerManager.RunTaskNow();
            Console.WriteLine(success ? "Task triggered successfully." : "Failed to trigger task.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            FreeConsole();
            return;
        }

        _mutex = new Mutex(initiallyOwned: true, "ScreenTimeController_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processesByName = Process.GetProcessesByName(currentProcess.ProcessName);
            bool foundActiveInstance = false;
            foreach (Process process in processesByName)
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr mainWindowHandle = process.MainWindowHandle;
                    if (mainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(mainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(mainWindowHandle);
                        foundActiveInstance = true;
                    }
                    else
                    {
                        try
                        {
                            process.Kill();
                            process.WaitForExit(3000);
                        }
                        catch
                        {
                            foundActiveInstance = true;
                        }
                    }
                    process.Dispose();
                    break;
                }
                process.Dispose();
            }
            if (foundActiveInstance)
            {
                MessageBox.Show(LanguageManager.GetString("AlreadyRunning"), LanguageManager.GetString("AlreadyRunningTitle"), MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                return;
            }
            _mutex?.Dispose();
            _mutex = new Mutex(initiallyOwned: true, "ScreenTimeController_SingleInstance_Mutex", out createdNew);
        }
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (ShouldShowInstallWizard())
            {
                using var wizard = new InstallWizardForm();
                wizard.ShowDialog();
            }

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error starting application: " + ex.Message + "\n\n" + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
        finally
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    private static void RestartAsAdmin(string[] args)
    {
        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Arguments = string.Join(" ", args),
                Verb = "runas",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to restart as administrator: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool ShouldShowInstallWizard()
    {
        try
        {
            bool serviceInstalled = TaskSchedulerManager.IsServiceInstalled();
            bool taskInstalled = TaskSchedulerManager.IsTaskInstalled();
            return !serviceInstalled || !taskInstalled;
        }
        catch
        {
            return false;
        }
    }
}
