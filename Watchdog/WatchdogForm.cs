using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScreenTimeControllerWatchdog
{
    public class WatchdogForm : Form
    {
        private const string MainProcessName = "ScreenTimeController";
        private readonly string _mainExePath;
        private readonly string _logPath;
        private volatile bool _isRunning;
        private Thread _watchdogThread;

        public WatchdogForm(string mainExePath, string logPath)
        {
            _mainExePath = mainExePath ?? "";
            _logPath = logPath ?? "";
            _isRunning = true;

            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(0, 0);
            this.Opacity = 0;

            Log("WatchdogForm created, mainExePath: " + _mainExePath);

            _watchdogThread = new Thread(WatchdogLoop)
            {
                IsBackground = false,
                Name = "WatchdogThread"
            };
            _watchdogThread.Start();
            Log("Watchdog thread started");
        }

        private void Log(string message)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, logMessage);
            }
            catch { }
        }

        private void WatchdogLoop()
        {
            Log("Watchdog loop started, waiting 1 second...");
            Thread.Sleep(1000);
            Log("Initial wait complete, starting monitoring...");

            while (_isRunning)
            {
                try
                {
                    bool mainProcessRunning = IsMainProcessRunning();
                    
                    if (!mainProcessRunning)
                    {
                        Thread.Sleep(200);
                        
                        if (!IsMainProcessRunning())
                        {
                            Log("Main process not found, restarting...");
                            RestartMainProcess();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Error in watchdog loop: " + ex.Message);
                }

                Thread.Sleep(500);
            }
            Log("Watchdog loop ended");
        }

        private bool IsMainProcessRunning()
        {
            Process[] processes = null;
            try
            {
                processes = Process.GetProcessesByName(MainProcessName);
                
                foreach (var p in processes)
                {
                    try
                    {
                        string processPath = p.MainModule?.FileName;
                        
                        if (string.IsNullOrEmpty(_mainExePath))
                        {
                            return true;
                        }
                        
                        if (processPath?.Equals(_mainExePath, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return true;
                        }
                    }
                    catch { }
                    finally
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log("Error in IsMainProcessRunning: " + ex.Message);
                return false;
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var p in processes)
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
            }
        }

        private void RestartMainProcess()
        {
            try
            {
                string exePath = _mainExePath;
                
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    exePath = FindMainExePath();
                }

                Log("Attempting to restart, exePath: " + exePath);
                
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                    Log("Main process restarted successfully");
                }
                else
                {
                    Log("Could not find main exe to restart");
                }
            }
            catch (Exception ex)
            {
                Log("Error restarting main process: " + ex.Message);
            }
        }

        private string FindMainExePath()
        {
            try
            {
                string currentDir = Path.GetDirectoryName(Application.ExecutablePath);
                string mainExePath = Path.Combine(currentDir, "ScreenTimeController.exe");
                Log("Looking for main exe at: " + mainExePath);
                
                if (File.Exists(mainExePath))
                {
                    return mainExePath;
                }

                string parentDir = Directory.GetParent(currentDir)?.FullName;
                if (parentDir != null)
                {
                    mainExePath = Path.Combine(parentDir, "ScreenTimeController.exe");
                    if (File.Exists(mainExePath))
                    {
                        return mainExePath;
                    }
                }

                Process[] processes = Process.GetProcessesByName(MainProcessName);
                foreach (var p in processes)
                {
                    try
                    {
                        string path = p.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            return path;
                        }
                    }
                    catch { }
                    finally
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
            }
            catch { }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isRunning = false;
            }
            base.Dispose(disposing);
        }
    }
}
