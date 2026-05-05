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

    public static bool SaveWithProtection(string fileName, string content)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string backupPath = Path.Combine(HiddenBackupDirectory, fileName);
        
        bool primarySuccess = SafeWriteFile(primaryPath, content);
        if (!primarySuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to write primary file: {primaryPath}");
            return false;
        }
        
        bool backupSuccess = SafeWriteFile(backupPath, content);
        if (!backupSuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to write backup file: {backupPath}");
        }
        
        bool registrySuccess = SaveToRegistry(fileName, content);
        if (!registrySuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to save to registry: {fileName}");
        }
        
        bool hashSuccess = SaveHash(fileName, content);
        if (!hashSuccess)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to save hash: {fileName}");
        }
        
        return primarySuccess;
    }

    public static void SaveFast(string fileName, string content)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        SafeWriteFile(primaryPath, content);
    }

    public static void SaveWithEncryption(string fileName, string content)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string encryptedContent = Convert.ToBase64String(ProtectedData.Protect(
            Encoding.UTF8.GetBytes(content),
            null,
            DataProtectionScope.CurrentUser));
        SafeWriteFile(primaryPath, encryptedContent);
    }

    public static string? LoadWithDecryption(string fileName)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string? encryptedContent = SafeReadFile(primaryPath);
        if (string.IsNullOrEmpty(encryptedContent))
        {
            return null;
        }
        try
        {
            byte[] decryptedBytes = ProtectedData.Unprotect(
                Convert.FromBase64String(encryptedContent),
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return null;
        }
    }

    public static string? LoadWithProtection(string fileName)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string backupPath = Path.Combine(HiddenBackupDirectory, fileName);
        
        string? content = SafeReadFile(primaryPath);
        if (!string.IsNullOrEmpty(content))
        {
            if (VerifyHash(fileName, content))
            {
                return content;
            }
            if (!HashExists(fileName))
            {
                return content;
            }
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Primary file hash verification failed for {fileName}");
        }
        
        content = SafeReadFile(backupPath);
        if (!string.IsNullOrEmpty(content))
        {
            if (VerifyHash(fileName, content))
            {
                RestorePrimaryFromBackup(fileName, content);
                return content;
            }
            if (!HashExists(fileName))
            {
                RestorePrimaryFromBackup(fileName, content);
                return content;
            }
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Backup file hash verification failed for {fileName}");
        }
        
        content = LoadFromRegistry(fileName);
        if (!string.IsNullOrEmpty(content))
        {
            if (VerifyHash(fileName, content))
            {
                RestorePrimaryFromBackup(fileName, content);
                return content;
            }
            if (!HashExists(fileName))
            {
                RestorePrimaryFromBackup(fileName, content);
                return content;
            }
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Registry backup hash verification failed for {fileName}");
        }
        
        content = SafeReadFile(primaryPath);
        if (!string.IsNullOrEmpty(content))
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Returning primary file content despite hash mismatch for {fileName}");
            SaveHash(fileName, content);
            return content;
        }
        
        System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] All data sources failed for {fileName}");
        return null;
    }

    private static bool HashExists(string fileName)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(HashRegistryKey);
            if (key == null)
            {
                return false;
            }
            string? storedHash = key.GetValue(fileName) as string;
            return !string.IsNullOrEmpty(storedHash);
        }
        catch
        {
            return false;
        }
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
        catch (Exception ex)
        {
            Logger.LogError($"Failed to record tampering for {fileName}", ex);
        }
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
        catch (Exception ex)
        {
            Logger.LogError("Failed to get tampering count", ex);
        }
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
        catch (Exception ex)
        {
            Logger.LogError("Failed to reset tampering count", ex);
        }
    }

    private static bool SaveHash(string fileName, string content)
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
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to create registry key for hash: {fileName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] SaveHash failed for {fileName}: {ex.Message}");
            return false;
        }
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

    private static bool SaveToRegistry(string fileName, string content)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.CreateSubKey(BackupRegistryKey, true);
            if (key != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(content);
                key.SetValue(fileName, data, RegistryValueKind.Binary);
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to create registry key for backup: {fileName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] SaveToRegistry failed for {fileName}: {ex.Message}");
            return false;
        }
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
                    return Encoding.UTF8.GetString(data);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load from registry: {fileName}", ex);
        }
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
        catch (Exception ex)
        {
            Logger.LogError($"Failed to restore primary from backup: {fileName}", ex);
        }
    }

    private static string? SafeReadFile(string filePath)
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
        catch
        {
            return null;
        }
    }

    private static bool SafeWriteFile(string filePath, string content)
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
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to set hidden attribute: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to create directory: {ex.Message}");
                return false;
            }
        }

        string tempFile = filePath + ".tmp";
        string backupFile = filePath + ".bak";
        
        try
        {
            File.WriteAllText(tempFile, content, Encoding.UTF8);
            
            if (File.Exists(filePath))
            {
                File.Replace(tempFile, filePath, backupFile);
                try
                {
                    if (File.Exists(backupFile))
                    {
                        File.Delete(backupFile);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Failed to delete backup file: {ex.Message}");
                }
            }
            else
            {
                File.Move(tempFile, filePath);
            }
            
            string verify = File.ReadAllText(filePath, Encoding.UTF8);
            if (verify != content)
            {
                System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] File verification failed for {filePath}");
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] SafeWriteFile failed for {filePath}: {ex.Message}");
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception deleteEx)
            {
                Logger.LogError($"Failed to delete temp file: {tempFile}", deleteEx);
            }
            return false;
        }
    }

    public static void EnsureDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
                SetDirectoryPermissions(DataDirectory);
            }
            
            if (!Directory.Exists(HiddenBackupDirectory))
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(HiddenBackupDirectory);
                dirInfo.Attributes |= FileAttributes.Hidden;
                SetDirectoryPermissions(HiddenBackupDirectory);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to ensure directory exists", ex);
        }
    }

    private static void SetDirectoryPermissions(string directoryPath)
    {
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
            DirectorySecurity security = dirInfo.GetAccessControl();
            
            SecurityIdentifier currentUserSid = WindowsIdentity.GetCurrent().User;
            if (currentUserSid != null)
            {
                security.AddAccessRule(new FileSystemAccessRule(
                    currentUserSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
            }
            
            try
            {
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
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] Cannot set restricted permissions, using current user permissions only");
            }
            
            dirInfo.SetAccessControl(security);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataProtectionManager] SetDirectoryPermissions failed: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            Logger.LogError($"Failed to check backup in registry: {fileName}", ex);
        }
        return false;
    }

    public static bool AreAllBackupsLost(string fileName)
    {
        string primaryPath = Path.Combine(DataDirectory, fileName);
        string backupPath = Path.Combine(HiddenBackupDirectory, fileName);

        bool primaryExists = File.Exists(primaryPath);
        bool backupExists = File.Exists(backupPath);
        bool registryExists = HasBackupInRegistry(fileName);

        return !primaryExists && !backupExists && !registryExists;
    }
}
