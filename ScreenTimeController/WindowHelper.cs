using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScreenTimeController;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern void LockWorkStation();

    public static string? GetActiveWindowProcessName()
    {
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return null;
            }
            GetWindowThreadProcessId(foregroundWindow, out uint lpdwProcessId);
            using Process process = Process.GetProcessById((int)lpdwProcessId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    public static bool ProcessHasWindow(string processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return false;
        }
        Process[]? array = null;
        try
        {
            array = Process.GetProcessesByName(processName);
            foreach (Process process in array)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (array != null)
            {
                foreach (Process process in array)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
            }
        }
    }

    public static bool HasOpenWindows()
    {
        Process[]? array = null;
        try
        {
            array = Process.GetProcesses();
            foreach (Process process in array)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (array != null)
            {
                foreach (Process process in array)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
            }
        }
    }
}
