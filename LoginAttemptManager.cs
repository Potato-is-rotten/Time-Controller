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
        bool loadedFromHklm = LoadFromRegistry(Registry.LocalMachine);
        
        if (!loadedFromHklm)
        {
            bool loadedFromHkcu = LoadFromRegistry(Registry.CurrentUser);
            if (!loadedFromHkcu)
            {
                LoadFromFile();
            }
        }
        
        if (LockedUntil.HasValue && LockedUntil.Value <= DateTime.Now)
        {
            ResetAttempts();
        }
    }

    private bool LoadFromRegistry(RegistryKey rootKey)
    {
        try
        {
            using RegistryKey? key = rootKey.OpenSubKey(RegistryKey);
            if (key != null)
            {
                FailedAttempts = (int)(key.GetValue("FailedAttempts", 0) ?? 0);
                long? lockTicks = key.GetValue("LockedUntil") as long?;
                if (lockTicks.HasValue && lockTicks.Value > 0)
                {
                    LockedUntil = new DateTime(lockTicks.Value);
                }
                return true;
            }
        }
        catch { }
        
        return false;
    }

    private void SaveAttempts()
    {
        bool savedToHklm = SaveToRegistry(Registry.LocalMachine);
        
        if (!savedToHklm)
        {
            SaveToRegistry(Registry.CurrentUser);
        }
        
        SaveToFile();
    }

    private bool SaveToRegistry(RegistryKey rootKey)
    {
        try
        {
            using RegistryKey? key = rootKey.CreateSubKey(RegistryKey, true);
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
                return true;
            }
        }
        catch { }
        
        return false;
    }

    private void SaveToFile()
    {
        try
        {
            DataProtectionManager.EnsureDirectoryExists();
            
            var data = new
            {
                FailedAttempts,
                LockedUntil = LockedUntil?.Ticks ?? 0
            };
            
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(AttemptsFilePath, json);
        }
        catch { }
    }

    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(AttemptsFilePath))
            {
                string json = File.ReadAllText(AttemptsFilePath);
                var data = JsonSerializer.Deserialize<LoginAttemptData>(json);
                if (data != null)
                {
                    FailedAttempts = data.FailedAttempts;
                    if (data.LockedUntil > 0)
                    {
                        LockedUntil = new DateTime(data.LockedUntil);
                    }
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
            Registry.CurrentUser.DeleteSubKeyTree(RegistryKey, false);
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

    private class LoginAttemptData
    {
        public int FailedAttempts { get; set; }
        public long LockedUntil { get; set; }
    }
}
