using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ScreenTimeController
{
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private int _sundayLimitMinutes;
        private int _mondayLimitMinutes;
        private int _tuesdayLimitMinutes;
        private int _wednesdayLimitMinutes;
        private int _thursdayLimitMinutes;
        private int _fridayLimitMinutes;
        private int _saturdayLimitMinutes;
        private string _passwordHash;
        private Language _language;

        public TimeSpan SundayLimit
        {
            get => TimeSpan.FromMinutes(_sundayLimitMinutes);
            set => _sundayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan MondayLimit
        {
            get => TimeSpan.FromMinutes(_mondayLimitMinutes);
            set => _mondayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan TuesdayLimit
        {
            get => TimeSpan.FromMinutes(_tuesdayLimitMinutes);
            set => _tuesdayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan WednesdayLimit
        {
            get => TimeSpan.FromMinutes(_wednesdayLimitMinutes);
            set => _wednesdayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan ThursdayLimit
        {
            get => TimeSpan.FromMinutes(_thursdayLimitMinutes);
            set => _thursdayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan FridayLimit
        {
            get => TimeSpan.FromMinutes(_fridayLimitMinutes);
            set => _fridayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public TimeSpan SaturdayLimit
        {
            get => TimeSpan.FromMinutes(_saturdayLimitMinutes);
            set => _saturdayLimitMinutes = Math.Min((int)value.TotalMinutes, 24 * 60);
        }

        public Language Language
        {
            get => _language;
            set
            {
                _language = value;
                LanguageManager.CurrentLanguage = value;
            }
        }

        public TimeSpan GetDailyLimit()
        {
            var dayOfWeek = DateTime.Today.DayOfWeek;
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => SundayLimit,
                DayOfWeek.Monday => MondayLimit,
                DayOfWeek.Tuesday => TuesdayLimit,
                DayOfWeek.Wednesday => WednesdayLimit,
                DayOfWeek.Thursday => ThursdayLimit,
                DayOfWeek.Friday => FridayLimit,
                DayOfWeek.Saturday => SaturdayLimit,
                _ => MondayLimit
            };
        }

        public SettingsManager()
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTimeController", "settings.txt");
            InitializeFile();
            LoadSettings();
        }

        private void InitializeFile()
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_settingsFilePath))
            {
                _sundayLimitMinutes = 120;
                _mondayLimitMinutes = 120;
                _tuesdayLimitMinutes = 120;
                _wednesdayLimitMinutes = 120;
                _thursdayLimitMinutes = 120;
                _fridayLimitMinutes = 120;
                _saturdayLimitMinutes = 120;
                _passwordHash = "";
                _language = Language.English;
                SaveSettings();
            }
        }

        private void LoadSettings()
        {
            _sundayLimitMinutes = 120;
            _mondayLimitMinutes = 120;
            _tuesdayLimitMinutes = 120;
            _wednesdayLimitMinutes = 120;
            _thursdayLimitMinutes = 120;
            _fridayLimitMinutes = 120;
            _saturdayLimitMinutes = 120;
            _language = Language.English;
            
            bool fileExists = File.Exists(_settingsFilePath);
            bool passwordLoaded = false;

            try
            {
                if (fileExists)
                {
                    var content = File.ReadAllText(_settingsFilePath);
                    var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            try
                            {
                                switch (parts[0].Trim())
                                {
                                    case "SundayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int sundayLimit))
                                            _sundayLimitMinutes = sundayLimit;
                                        break;
                                    case "MondayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int mondayLimit))
                                            _mondayLimitMinutes = mondayLimit;
                                        break;
                                    case "TuesdayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int tuesdayLimit))
                                            _tuesdayLimitMinutes = tuesdayLimit;
                                        break;
                                    case "WednesdayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int wednesdayLimit))
                                            _wednesdayLimitMinutes = wednesdayLimit;
                                        break;
                                    case "ThursdayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int thursdayLimit))
                                            _thursdayLimitMinutes = thursdayLimit;
                                        break;
                                    case "FridayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int fridayLimit))
                                            _fridayLimitMinutes = fridayLimit;
                                        break;
                                    case "SaturdayLimit":
                                        if (int.TryParse(parts[1].Trim(), out int saturdayLimit))
                                            _saturdayLimitMinutes = saturdayLimit;
                                        break;
                                    case "PasswordHash":
                                        string hashValue = parts[1].Trim();
                                        if (!string.IsNullOrEmpty(hashValue))
                                        {
                                            _passwordHash = hashValue;
                                            passwordLoaded = true;
                                        }
                                        break;
                                    case "Language":
                                        if (int.TryParse(parts[1].Trim(), out int langValue) && Enum.IsDefined(typeof(Language), langValue))
                                            _language = (Language)langValue;
                                        break;
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
            catch
            {
                _sundayLimitMinutes = 120;
                _mondayLimitMinutes = 120;
                _tuesdayLimitMinutes = 120;
                _wednesdayLimitMinutes = 120;
                _thursdayLimitMinutes = 120;
                _fridayLimitMinutes = 120;
                _saturdayLimitMinutes = 120;
                _language = Language.English;
            }

            if (!passwordLoaded)
            {
                _passwordHash = "";
            }

            LanguageManager.CurrentLanguage = _language;
        }

        public void SaveSettings()
        {
            try
            {
                var content = $"SundayLimit={_sundayLimitMinutes}{Environment.NewLine}";
                content += $"MondayLimit={_mondayLimitMinutes}{Environment.NewLine}";
                content += $"TuesdayLimit={_tuesdayLimitMinutes}{Environment.NewLine}";
                content += $"WednesdayLimit={_wednesdayLimitMinutes}{Environment.NewLine}";
                content += $"ThursdayLimit={_thursdayLimitMinutes}{Environment.NewLine}";
                content += $"FridayLimit={_fridayLimitMinutes}{Environment.NewLine}";
                content += $"SaturdayLimit={_saturdayLimitMinutes}{Environment.NewLine}";
                content += $"PasswordHash={_passwordHash}{Environment.NewLine}";
                content += $"Language={(int)_language}";
                File.WriteAllText(_settingsFilePath, content);
            }
            catch
            {
            }
        }

        public bool HasPassword()
        {
            return !string.IsNullOrEmpty(_passwordHash);
        }

        public bool VerifyPassword(string password)
        {
            try
            {
                if (string.IsNullOrEmpty(_passwordHash))
                {
                    return true;
                }
                
                if (string.IsNullOrEmpty(password))
                {
                    return false;
                }
                
                string inputHash = HashPassword(password);
                return _passwordHash == inputHash;
            }
            catch
            {
                return false;
            }
        }

        public void SetPassword(string password)
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

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "";
            }
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
