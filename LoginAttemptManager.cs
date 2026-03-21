using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace ScreenTimeController;

public class LoginAttemptManager
{
    private const string RegistryKey = @"SOFTWARE\ScreenTimeController\LoginAttempts";
    private const int MaxAttempts = 5;
    
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ScreenTimeController");
    
    private static readonly string AttemptsFilePath = Path.Combine(DataDirectory, "login_attempts.json");

    public int FailedAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.Now;

    public LoginAttemptManager()
    {
        LoadAttempts();
    }

    public void RecordFailedAttempt()
    {
        FailedAttempts++;
        CheckAndApplyLock();
        SaveAttempts();
    }

    public void ResetAttempts()
    {
        FailedAttempts = 0;
        LockedUntil = null;
        SaveAttempts();
    }

    public bool CanAttemptLogin()
    {
        if (!IsLocked)
        {
            return true;
        }
        
        if (LockedUntil.HasValue && LockedUntil.Value <= DateTime.Now)
        {
            ResetAttempts();
            return true;
        }
        
        return false;
    }

    public TimeSpan? GetRemainingLockTime()
    {
        if (!IsLocked || !LockedUntil.HasValue)
        {
            return null;
        }
        
        return LockedUntil.Value - DateTime.Now;
    }

    private void CheckAndApplyLock()
    {
        if (FailedAttempts >= MaxAttempts)
        {
            DateTime tomorrow = DateTime.Today.AddDays(1);
            LockedUntil = tomorrow;
        }
    }

    private void LoadAttempts()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(RegistryKey);
            if (key != null)
            {
                FailedAttempts = (int)(key.GetValue("FailedAttempts", 0) ?? 0);
                long? lockTicks = key.GetValue("LockedUntil") as long?;
                if (lockTicks.HasValue && lockTicks.Value > 0)
                {
                    LockedUntil = new DateTime(lockTicks.Value);
                }
                
                if (LockedUntil.HasValue && LockedUntil.Value <= DateTime.Now)
                {
                    ResetAttempts();
                }
                return;
            }
        }
        catch { }
        
        FailedAttempts = 0;
        LockedUntil = null;
    }

    private void SaveAttempts()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(RegistryKey, true);
            if (key != null)
            {
                key.SetValue("FailedAttempts", FailedAttempts);
                if (LockedUntil.HasValue)
                {
                    key.SetValue("LockedUntil", LockedUntil.Value.Ticks);
                }
                else
                {
                    key.SetValue("LockedUntil", 0);
                }
            }
        }
        catch { }
    }

    public static void ClearAllData()
    {
        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(RegistryKey, false);
        }
        catch { }
        
        try
        {
            if (File.Exists(AttemptsFilePath))
            {
                File.Delete(AttemptsFilePath);
            }
        }
        catch { }
    }
}
