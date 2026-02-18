using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScreenTimeController
{
    public static class Watchdog
    {
        private const string MainProcessName = "ScreenTimeController";
        private const string WatchdogProcessName = "ScreenTimeControllerWatchdog";
        
        private static string _mainExePath = "";
        private static SettingsManager _settingsManager;
        private static volatile bool _isRunning;
        private static Thread _monitorThread;
        private static string _logPath = "";
        private static string _watchdogPath = "";
        private static readonly object _lockObj = new object();

        public static void Start(string mainExePath, SettingsManager settingsManager)
        {
            lock (_lockObj)
            {
                if (_isRunning) return;
                
                _mainExePath = mainExePath ?? "";
                _settingsManager = settingsManager;
                _isRunning = true;
                
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScreenTimeController"
                );
                
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
                
                _logPath = Path.Combine(appDataPath, "watchdog_monitor.log");
                
                _watchdogPath = FindWatchdogPath();
                Log("Watchdog path: " + _watchdogPath);
                
                StartWatchdogProcess();
                
                _monitorThread = new Thread(MonitorWatchdogLoop)
                {
                    IsBackground = true,
                    Name = "WatchdogMonitor"
                };
                _monitorThread.Start();
            }
        }

        public static void Stop()
        {
            lock (_lockObj)
            {
                _isRunning = false;
            }
        }

        private static string FindWatchdogPath()
        {
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.StartupPath, "ScreenTimeControllerWatchdog.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScreenTimeControllerWatchdog.exe"),
                Path.Combine(Directory.GetParent(Application.StartupPath)?.FullName ?? "", "ScreenTimeControllerWatchdog.exe"),
            };

            foreach (var path in possiblePaths)
            {
                Log("Checking watchdog path: " + path);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return "";
        }

        private static void StartWatchdogProcess()
        {
            if (string.IsNullOrEmpty(_watchdogPath) || !File.Exists(_watchdogPath))
            {
                Log("Watchdog executable not found");
                return;
            }

            try
            {
                if (IsWatchdogRunning())
                {
                    Log("Watchdog already running");
                    return;
                }

                string watchdogDir = Path.GetDirectoryName(_watchdogPath);
                
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = _watchdogPath,
                    Arguments = $"\"{_mainExePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = watchdogDir
                };

                Process.Start(startInfo);
                Log("Watchdog process started");
            }
            catch (Exception ex)
            {
                Log("Error starting watchdog: " + ex.Message);
            }
        }

        private static bool IsWatchdogRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(WatchdogProcessName);
                bool isRunning = processes.Length > 0;
                
                foreach (var p in processes)
                {
                    try { p.Dispose(); } catch { }
                }
                
                return isRunning;
            }
            catch
            {
                return false;
            }
        }

        private static void MonitorWatchdogLoop()
        {
            Thread.Sleep(2000);
            
            while (_isRunning)
            {
                try
                {
                    if (!IsWatchdogRunning())
                    {
                        Log("Watchdog not running, restarting...");
                        Thread.Sleep(200);
                        if (!IsWatchdogRunning())
                        {
                            StartWatchdogProcess();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Error in monitor loop: " + ex.Message);
                }

                Thread.Sleep(500);
            }
            
            Log("Monitor loop ended");
        }

        private static void Log(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, logMessage);
            }
            catch { }
        }
    }
}
