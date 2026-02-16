using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScreenTimeController
{
    public static class WindowHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        public static string GetActiveWindowProcessName()
        {
            try
            {
                var hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                    return null;

                GetWindowThreadProcessId(hWnd, out uint processId);
                using (var process = Process.GetProcessById((int)processId))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool ProcessHasWindow(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return false;

            Process[] processes = null;
            try
            {
                processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var p in processes)
                    {
                        try
                        {
                            p.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        public static bool HasOpenWindows()
        {
            Process[] processes = null;
            try
            {
                processes = Process.GetProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var p in processes)
                    {
                        try
                        {
                            p.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}
