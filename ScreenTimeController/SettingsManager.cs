using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ScreenTimeController;

public class SettingsManager
{
    private const string SettingsFileName = "settings.txt";
    private readonly string _settingsFilePath;
    private readonly string _backupFilePath;
    private readonly object _lockObject = new();

    private int _sundayLimitMinutes = 120;
    private int _mondayLimitMinutes = 120;
    private int _tuesdayLimitMinutes = 120;
    private int _wednesdayLimitMinutes = 120;
    private int _thursdayLimitMinutes = 120;
    private int _fridayLimitMinutes = 120;
    private int _saturdayLimitMinutes = 120;
    private string _passwordHash = "";
    private Language _language = Language.English;
    private bool _enablePasswordLock = true;

    public TimeSpan SundayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_sundayLimitMinutes); } }
        set { lock (_lockObject) { _sundayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan MondayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_mondayLimitMinutes); } }
        set { lock (_lockObject) { _mondayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan TuesdayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_tuesdayLimitMinutes); } }
        set { lock (_lockObject) { _tuesdayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan WednesdayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_wednesdayLimitMinutes); } }
        set { lock (_lockObject) { _wednesdayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan ThursdayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_thursdayLimitMinutes); } }
        set { lock (_lockObject) { _thursdayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan FridayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_fridayLimitMinutes); } }
        set { lock (_lockObject) { _fridayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public TimeSpan SaturdayLimit
    {
        get { lock (_lockObject) { return TimeSpan.FromMinutes(_saturdayLimitMinutes); } }
        set { lock (_lockObject) { _saturdayLimitMinutes = Math.Min((int)value.TotalMinutes, 1440); } }
    }

    public Language Language
    {
        get { lock (_lockObject) { return _language; } }
        set
        {
            lock (_lockObject) { _language = value; }
            LanguageManager.CurrentLanguage = value;
        }
    }

    public bool EnablePasswordLock
    {
        get { lock (_lockObject) { return _enablePasswordLock; } }
        set { lock (_lockObject) { _enablePasswordLock = value; } }
    }

    public TimeSpan GetDailyLimit()
    {
        return DateTime.Today.DayOfWeek switch
        {
            DayOfWeek.Sunday => SundayLimit,
            DayOfWeek.Monday => MondayLimit,
            DayOfWeek.Tuesday => TuesdayLimit,
            DayOfWeek.Wednesday => WednesdayLimit,
            DayOfWeek.Thursday => ThursdayLimit,
            DayOfWeek.Friday => FridayLimit,
            DayOfWeek.Saturday => SaturdayLimit,
            _ => MondayLimit,
        };
    }

    public SettingsManager()
    {
        _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController", "settings.txt");
        _backupFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController", "settings_backup.txt");
        DataProtectionManager.EnsureDirectoryExists();
        LoadSettings();
    }

    private void EnsureDirectory()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch { }
    }

    private string? SafeReadFile(string filePath)
    {
        for (int i = 0; i < 5; i++)
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

    private void SafeWriteFile(string filePath, string content)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            try { Directory.CreateDirectory(directory); } catch { }
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
                
                string verify = File.ReadAllText(filePath, Encoding.UTF8);
                if (verify == content)
                {
                    return;
                }
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(50);
            }
            catch
            {
                return;
            }
        }
    }

    private void LoadSettings()
    {
        lock (_lockObject)
        {
            bool loaded = false;
            try
            {
                string? content = DataProtectionManager.LoadWithProtection(SettingsFileName);

                if (!string.IsNullOrEmpty(content))
                {
                    ParseSettings(content);
                    loaded = true;
                }
                else
                {
                    content = DataProtectionManager.LoadWithDecryption(SettingsFileName);
                    if (!string.IsNullOrEmpty(content))
                    {
                        ParseSettings(content);
                        SaveWithEncryption();
                        loaded = true;
                    }
                }
            }
            catch { }

            if (!loaded)
            {
                bool backupsLost = false;
                try { backupsLost = DataProtectionManager.AreAllBackupsLost(SettingsFileName); } catch { }
                
                if (!backupsLost)
                {
                    try { SaveSettings(); } catch { }
                }
            }
        }

        LanguageManager.CurrentLanguage = _language;
    }

    private void SaveWithEncryption()
    {
        try
        {
            EnsureDirectory();

            string content;
            lock (_lockObject)
            {
                StringBuilder sb = new();
                sb.AppendLine($"SundayLimit={_sundayLimitMinutes}");
                sb.AppendLine($"MondayLimit={_mondayLimitMinutes}");
                sb.AppendLine($"TuesdayLimit={_tuesdayLimitMinutes}");
                sb.AppendLine($"WednesdayLimit={_wednesdayLimitMinutes}");
                sb.AppendLine($"ThursdayLimit={_thursdayLimitMinutes}");
                sb.AppendLine($"FridayLimit={_fridayLimitMinutes}");
                sb.AppendLine($"SaturdayLimit={_saturdayLimitMinutes}");
                sb.AppendLine($"PasswordHash={_passwordHash}");
                sb.AppendLine($"Language={(int)_language}");
                sb.Append($"EnablePasswordLock={_enablePasswordLock}");
                content = sb.ToString();
            }

            DataProtectionManager.SaveWithEncryption(SettingsFileName, content);
        }
        catch { }
    }

    private void ParseSettings(string content)
    {
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] parts = line.Split(new[] { '=' }, 2);
            if (parts.Length != 2)
            {
                continue;
            }

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            try
            {
                switch (key)
                {
                    case "SundayLimit":
                        if (int.TryParse(value, out int sun)) _sundayLimitMinutes = sun;
                        break;
                    case "MondayLimit":
                        if (int.TryParse(value, out int mon)) _mondayLimitMinutes = mon;
                        break;
                    case "TuesdayLimit":
                        if (int.TryParse(value, out int tue)) _tuesdayLimitMinutes = tue;
                        break;
                    case "WednesdayLimit":
                        if (int.TryParse(value, out int wed)) _wednesdayLimitMinutes = wed;
                        break;
                    case "ThursdayLimit":
                        if (int.TryParse(value, out int thu)) _thursdayLimitMinutes = thu;
                        break;
                    case "FridayLimit":
                        if (int.TryParse(value, out int fri)) _fridayLimitMinutes = fri;
                        break;
                    case "SaturdayLimit":
                        if (int.TryParse(value, out int sat)) _saturdayLimitMinutes = sat;
                        break;
                    case "PasswordHash":
                        if (!string.IsNullOrEmpty(value))
                        {
                            _passwordHash = value;
                        }
                        break;
                    case "Language":
                        if (int.TryParse(value, out int lang) && Enum.IsDefined(typeof(Language), lang))
                        {
                            _language = (Language)lang;
                        }
                        break;
                    case "EnablePasswordLock":
                        if (bool.TryParse(value, out bool enableLock))
                        {
                            _enablePasswordLock = enableLock;
                        }
                        break;
                }
            }
            catch { }
        }
    }

    public void SaveSettings()
    {
        try
        {
            EnsureDirectory();

            string content;
            lock (_lockObject)
            {
                StringBuilder sb = new();
                sb.AppendLine($"SundayLimit={_sundayLimitMinutes}");
                sb.AppendLine($"MondayLimit={_mondayLimitMinutes}");
                sb.AppendLine($"TuesdayLimit={_tuesdayLimitMinutes}");
                sb.AppendLine($"WednesdayLimit={_wednesdayLimitMinutes}");
                sb.AppendLine($"ThursdayLimit={_thursdayLimitMinutes}");
                sb.AppendLine($"FridayLimit={_fridayLimitMinutes}");
                sb.AppendLine($"SaturdayLimit={_saturdayLimitMinutes}");
                sb.AppendLine($"PasswordHash={_passwordHash}");
                sb.AppendLine($"Language={(int)_language}");
                sb.Append($"EnablePasswordLock={_enablePasswordLock}");
                content = sb.ToString();
            }

            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    File.Copy(_settingsFilePath, _backupFilePath, true);
                }
                catch { }
            }

            DataProtectionManager.SaveFast(SettingsFileName, content);
        }
        catch { }
    }

    public bool HasPassword()
    {
        lock (_lockObject)
        {
            return !string.IsNullOrEmpty(_passwordHash);
        }
    }

    public bool VerifyPassword(string password)
    {
        try
        {
            lock (_lockObject)
            {
                if (string.IsNullOrEmpty(_passwordHash))
                {
                    return true;
                }
                if (string.IsNullOrEmpty(password))
                {
                    return false;
                }
                string hash = HashPassword(password);
                return _passwordHash == hash;
            }
        }
        catch
        {
            return false;
        }
    }

    public void SetPassword(string password)
    {
        lock (_lockObject)
        {
            if (string.IsNullOrEmpty(password))
            {
                _passwordHash = "";
            }
            else
            {
                _passwordHash = HashPassword(password);
            }
        }
        SaveSettings();
    }

    private static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "";
        }
        using SHA256 sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public void ResetToDefaults()
    {
        lock (_lockObject)
        {
            _sundayLimitMinutes = 120;
            _mondayLimitMinutes = 120;
            _tuesdayLimitMinutes = 120;
            _wednesdayLimitMinutes = 120;
            _thursdayLimitMinutes = 120;
            _fridayLimitMinutes = 120;
            _saturdayLimitMinutes = 120;
            _passwordHash = "";
            _language = Language.SimplifiedChinese;
            _enablePasswordLock = false;
        }
        SaveSettings();
    }
}
