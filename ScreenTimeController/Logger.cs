using System;
using System.Diagnostics;

namespace ScreenTimeController;

public static class Logger
{
    public static void LogInfo(string message)
    {
        Debug.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    public static void LogWarning(string message)
    {
        Debug.WriteLine($"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    public static void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}: {exception.Message}\n{exception.StackTrace}");
        }
        else
        {
            Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }

    public static void LogDebug(string message)
    {
        Debug.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }
}
