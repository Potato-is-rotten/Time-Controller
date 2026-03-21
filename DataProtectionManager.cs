using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace ScreenTimeController;

public static class DataProtectionManager
{
    private const string RegistryBaseKey = @"SOFTWARE\ScreenTimeController";
    private const string HashRegistryKey = @"SOFTWARE\ScreenTimeController\Hashes";
    private const string BackupRegistryKey = @"SOFTWARE\ScreenTimeController\Backup";
    
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ScreenTimeController");
    
    private static readonly string HiddenBackupDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ScreenTimeController",
        ".backup");

    public static void SaveWithProtection(string fileName, string content)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string backupPath = Path.Combine(HiddenBackupDirectory, fileName);
        
        SafeWriteFile(primaryPath, content);
        SafeWriteFile(backupPath, content);
        SaveToRegistry(fileName, content);
        SaveHash(fileName, content);
    }

    public static string? LoadWithProtection(string fileName)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string backupPath = Path.Combine(HiddenBackupDirectory, fileName);
        
        string? content = SafeReadFile(primaryPath);
        if (!string.IsNullOrEmpty(content) && VerifyHash(fileName, content))
        {
            return content;
        }
        
        content = SafeReadFile(backupPath);
        if (!string.IsNullOrEmpty(content) && VerifyHash(fileName, content))
        {
            RestorePrimaryFromBackup(fileName, content);
            return content;
        }
        
        content = LoadFromRegistry(fileName);
        if (!string.IsNullOrEmpty(content) && VerifyHash(fileName, content))
        {
            RestorePrimaryFromBackup(fileName, content);
            return content;
        }
        
        return null;
    }

    public static bool VerifyIntegrity(string fileName)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string? content = SafeReadFile(primaryPath);
        
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }
        
        return VerifyHash(fileName, content);
    }

    public static void RecordTampering(string fileName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(BackupRegistryKey, true);
            if (key != null)
            {
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                string existingLog = key.GetValue("TamperingLog", "") as string ?? "";
                string newEntry = $"[{timestamp}] {fileName}";
                key.SetValue("TamperingLog", existingLog + newEntry + Environment.NewLine);
                
                int count = (int)(key.GetValue("TamperingCount", 0) ?? 0);
                key.SetValue("TamperingCount", count + 1);
            }
        }
        catch { }
    }

    public static int GetTamperingCount()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(BackupRegistryKey);
            if (key != null)
            {
                return (int)(key.GetValue("TamperingCount", 0) ?? 0);
            }
        }
        catch { }
        return 0;
    }

    public static void ResetTamperingCount()
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(BackupRegistryKey, true);
            if (key != null)
            {
                key.SetValue("TamperingCount", 0);
            }
        }
        catch { }
    }

    private static void SaveHash(string fileName, string content)
    {
        try
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            string hashHex = Convert.ToHexString(hashBytes);
            
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(HashRegistryKey, true);
            if (key != null)
            {
                key.SetValue(fileName, hashHex);
                key.SetValue($"{fileName}_Time", DateTime.UtcNow.Ticks);
            }
        }
        catch { }
    }

    private static bool VerifyHash(string fileName, string content)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(HashRegistryKey);
            if (key == null)
            {
                return true;
            }
            
            string? storedHash = key.GetValue(fileName) as string;
            if (string.IsNullOrEmpty(storedHash))
            {
                return true;
            }
            
            byte[] currentHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
            string currentHashHex = Convert.ToHexString(currentHashBytes);
            
            return storedHash.Equals(currentHashHex, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true;
        }
    }

    private static void SaveToRegistry(string fileName, string content)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(BackupRegistryKey, true);
            if (key != null)
            {
                byte[] compressed = Compress(content);
                key.SetValue(fileName, compressed, RegistryValueKind.Binary);
            }
        }
        catch { }
    }

    private static string? LoadFromRegistry(string fileName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(BackupRegistryKey);
            if (key != null)
            {
                byte[]? data = key.GetValue(fileName) as byte[];
                if (data != null && data.Length > 0)
                {
                    return Decompress(data);
                }
            }
        }
        catch { }
        return null;
    }

    private static void RestorePrimaryFromBackup(string fileName, string content)
    {
        try
        {
            string primaryPath = Path.Combine(DataDirectory, fileName);
            SafeWriteFile(primaryPath, content);
            RecordTampering(fileName);
        }
        catch { }
    }

    private static byte[] Compress(string content)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);
        return data;
    }

    private static string Decompress(byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    private static string? SafeReadFile(string filePath)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }
                
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(50);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static void SafeWriteFile(string filePath, string content)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try
            {
                Directory.CreateDirectory(directory);
                
                if (directory.Contains(".backup"))
                {
                    try
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(directory);
                        dirInfo.Attributes |= FileAttributes.Hidden;
                    }
                    catch { }
                }
            }
            catch { }
        }

        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                string tempFile = filePath + ".tmp";
                File.WriteAllText(tempFile, content, Encoding.UTF8);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                File.Move(tempFile, filePath);
                return;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException)
            {
                System.Threading.Thread.Sleep(50);
            }
            catch
            {
                return;
            }
        }
    }

    public static void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(DataDirectory);
                SetDirectoryPermissions(DataDirectory);
            }
            
            if (!Directory.Exists(HiddenBackupDirectory))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(HiddenBackupDirectory);
                dirInfo.Attributes |= FileAttributes.Hidden;
                SetDirectoryPermissions(HiddenBackupDirectory);
            }
        }
        catch { }
    }

    private static void SetDirectoryPermissions(string directoryPath)
    {
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            DirectorySecurity security = dirInfo.GetAccessControl();
            
            security.SetAccessRuleProtection(true, false);
            
            SecurityIdentifier adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            security.AddAccessRule(new FileSystemAccessRule(
                adminSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            
            SecurityIdentifier systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            security.AddAccessRule(new FileSystemAccessRule(
                systemSid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            
            dirInfo.SetAccessControl(security);
        }
        catch { }
    }

    public static bool HasBackupInRegistry(string fileName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(BackupRegistryKey);
            if (key != null)
            {
                return key.GetValue(fileName) != null;
            }
        }
        catch { }
        return false;
    }
}
