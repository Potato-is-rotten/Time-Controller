using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ScreenTimeControllerWatchdog
{
    static class Program
    {
        private const string MutexName = "ScreenTimeControllerWatchdog_SingleInstance";
        
        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ScreenTimeController",
                    "watchdog_external.log"
                );
                
                try
                {
                    Log(logPath, "Program starting...");
                    Log(logPath, "Args: " + (args.Length > 0 ? args[0] : "none"));
                    
                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    
                    string mainExePath = args.Length > 0 ? args[0] : "";
                    
                    Application.Run(new WatchdogForm(mainExePath, logPath));
                }
                catch (Exception ex)
                {
                    Log(logPath, "FATAL ERROR: " + ex.ToString());
                }
            }
        }
        
        private static void Log(string logPath, string message)
        {
            try
            {
                string dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch { }
        }
    }
}
