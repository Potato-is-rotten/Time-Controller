using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace WatchdogMonitor
{
    class Program
    {
        private const string ServiceName = "ScreenTimeController_ProtectionService";
        private const string MainProcessName = "ScreenTimeController";
        private const string ProtectionServiceProcessName = "ProtectionService";
        private const string WatchdogProcessName = "WatchdogMonitor";
        private const string MutexName = "ScreenTimeController_WatchdogMonitor_SingleInstance";

        private static string? _exePath;
        private static string? _protectionServicePath;
        private static string? _cleanExitFile;
        private static Timer? _monitorTimer;
        private static bool _isRunning;
        private static DateTime _lastCleanExitDate = DateTime.MinValue;
        private static Mutex? _mutex;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private const int SW_HIDE = 0;

        static int Main(string[] args)
        {
            try
            {
                _mutex = new Mutex(true, MutexName, out bool createdNew);
                if (!createdNew)
                {
                    return 0;
                }

                IntPtr consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_HIDE);
                }

                string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                _exePath = Path.Combine(currentDir ?? "", "ScreenTimeController.exe");
                _protectionServicePath = Path.Combine(currentDir ?? "", "ProtectionService.exe");
                _cleanExitFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController", "clean_exit.txt");

                Log("WatchdogMonitor starting...");
                Log($"ExePath: {_exePath}");
                Log($"ProtectionServicePath: {_protectionServicePath}");
                Log($"CleanExitFile: {_cleanExitFile}");

                CheckAndRestartService(_protectionServicePath);
                CheckAndRestartMainProgram(_exePath, _cleanExitFile);

                if (args.Length > 0 && args[0].Equals("--single", StringComparison.OrdinalIgnoreCase))
                {
                    Log("Running in single check mode. Exiting.");
                    _mutex.ReleaseMutex();
                    return 0;
                }

                _monitorTimer = new Timer(MonitorCallback, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

                Log("WatchdogMonitor started. Monitoring every 10 seconds...");

                while (true)
                {
                    Thread.Sleep(60000);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    string logPath = Path.Combine(logDir, "watchdog_error.log");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL: {ex.Message}{Environment.NewLine}");
                }
                catch { }
                return 1;
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        private static void MonitorCallback(object? state)
        {
            if (_isRunning) return;
            _isRunning = true;
            try
            {
                CheckAndRestartService(_protectionServicePath);
                CheckAndRestartMainProgram(_exePath, _cleanExitFile);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private static void CheckAndRestartService(string? protectionServicePath)
        {
            try
            {
                bool serviceInstalled = IsServiceInstalled();
                bool serviceRunning = false;

                if (serviceInstalled)
                {
                    serviceRunning = IsServiceRunning();
                }

                if (!serviceRunning)
                {
                    Process[] processes = Process.GetProcessesByName(ProtectionServiceProcessName);
                    if (processes.Length == 0)
                    {
                        Log("Protection service not running. Starting...");
                        if (serviceInstalled)
                        {
                            StartServiceViaSc();
                        }
                        else if (!string.IsNullOrEmpty(protectionServicePath) && File.Exists(protectionServicePath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = protectionServicePath,
                                UseShellExecute = true,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            });
                            Log("Started ProtectionService.exe directly.");
                        }
                    }
                    else
                    {
                        foreach (Process p in processes)
                        {
                            p.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking/restarting service: {ex.Message}");
            }
        }

        private static void CheckAndRestartMainProgram(string? exePath, string? cleanExitFile)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(MainProcessName);
                if (processes.Length == 0)
                {
                    bool shouldRestart = true;

                    if (!string.IsNullOrEmpty(cleanExitFile) && File.Exists(cleanExitFile))
                    {
                        try
                        {
                            string content = File.ReadAllText(cleanExitFile).Trim();
                            if (DateTime.TryParse(content, out DateTime cleanExitDate))
                            {
                                if (cleanExitDate.Date == DateTime.Today || cleanExitDate.Date == DateTime.Today.AddDays(-1))
                                {
                                    if (_lastCleanExitDate != cleanExitDate)
                                    {
                                        _lastCleanExitDate = cleanExitDate;
                                        Log($"Clean exit detected at {cleanExitDate}. Waiting for user to restart manually.");
                                    }
                                    shouldRestart = false;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error reading clean exit file: {ex.Message}");
                        }
                    }

                    if (shouldRestart && !string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        Log("Main program not running and no clean exit today. Restarting...");
                        RecordAbnormalExit();

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true
                        });
                        Log("Main program restart initiated.");
                    }
                }
                else
                {
                    foreach (Process p in processes)
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking/restarting main program: {ex.Message}");
            }
        }

        private static bool IsServiceInstalled()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}");
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsServiceRunning()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}");
                if (key == null) return false;

                object? status = key.GetValue("Start");
                if (status != null && (int)status == 4)
                {
                    return false;
                }

                Process[] processes = Process.GetProcessesByName(ProtectionServiceProcessName);
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

        private static bool StartServiceViaSc()
        {
            try
            {
                string scPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");

                ProcessStartInfo startInfo = new()
                {
                    FileName = scPath,
                    Arguments = $"start \"{ServiceName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process? process = Process.Start(startInfo);
                process?.WaitForExit();

                Log("Attempted to start service via sc.exe");
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Log($"Failed to start service: {ex.Message}");
                return false;
            }
        }

        private static void RecordAbnormalExit()
        {
            try
            {
                string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
                if (!Directory.Exists(commonDataDir))
                {
                    Directory.CreateDirectory(commonDataDir);
                }

                string abnormalExitFile = Path.Combine(commonDataDir, "abnormal_exits.txt");
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                int count = 0;

                if (File.Exists(abnormalExitFile))
                {
                    string[] lines = File.ReadAllLines(abnormalExitFile);
                    if (lines.Length > 0 && lines[0].StartsWith(today))
                    {
                        if (lines.Length > 1 && int.TryParse(lines[1], out count))
                        {
                            count++;
                        }
                        else
                        {
                            count = 1;
                        }
                    }
                    else
                    {
                        count = 1;
                    }
                }
                else
                {
                    count = 1;
                }

                File.WriteAllLines(abnormalExitFile, new string[] { today, count.ToString() });
                Log($"Recorded abnormal exit. Today's count: {count}");
            }
            catch (Exception ex)
            {
                Log($"Error recording abnormal exit: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logPath = Path.Combine(logDir, "watchdog.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch { }
        }
    }
}
