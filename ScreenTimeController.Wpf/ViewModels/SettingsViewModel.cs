using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ScreenTimeController.Wpf.ViewModels;

public class DayLimitItem : ViewModelBase
{
    public string DayName { get; set; } = "";
    
    private int _hours;
    public int Hours
    {
        get => _hours;
        set
        {
            if (SetProperty(ref _hours, value))
            {
                TotalMinutes = Hours * 60 + Minutes;
            }
        }
    }
    
    private int _minutes;
    public int Minutes
    {
        get => _minutes;
        set
        {
            if (SetProperty(ref _minutes, value))
            {
                TotalMinutes = Hours * 60 + Minutes;
            }
        }
    }
    
    private int _totalMinutes;
    public int TotalMinutes
    {
        get => _totalMinutes;
        set => SetProperty(ref _totalMinutes, value);
    }
}

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;
    private bool _isPasswordVerified;
    private bool _isClosing;
    private int _selectedLanguageIndex;
    private bool _enablePasswordLock;
    private string _statusMessage = "";

    public ObservableCollection<DayLimitItem> DayLimits { get; } = new();

    public List<string> Languages { get; } = new()
    {
        "English",
        "简体中文",
        "繁體中文",
        "日本語",
        "한국어",
        "Español",
        "Français",
        "Deutsch",
        "Русский",
        "Português"
    };

    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set => SetProperty(ref _selectedLanguageIndex, value);
    }

    public bool EnablePasswordLock
    {
        get => _enablePasswordLock;
        set => SetProperty(ref _enablePasswordLock, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OKCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ChangePasswordCommand { get; }
    public ICommand ApplyToAllCommand { get; }

    public event EventHandler? SettingsSaved;
    public event EventHandler? Cancelled;
    public event EventHandler? RequestChangePassword;
    public event EventHandler? RequestPasswordVerify;

    public bool IsPasswordVerified => _isPasswordVerified;

    public SettingsViewModel(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        OKCommand = new RelayCommand(ExecuteOK, CanExecuteOK);
        CancelCommand = new RelayCommand(ExecuteCancel);
        ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        ApplyToAllCommand = new RelayCommand(ExecuteApplyToAll, CanExecuteApplyToAll);
        
        LoadSettings();
    }

    public bool VerifyPassword()
    {
        if (!_settingsManager.HasPassword())
        {
            _isPasswordVerified = true;
            return true;
        }
        return false;
    }

    public void SetPasswordVerified()
    {
        _isPasswordVerified = true;
    }

    private void LoadSettings()
    {
        DayLimits.Clear();
        
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Sunday"), 
            Hours = (int)_settingsManager.SundayLimit.TotalHours, 
            Minutes = _settingsManager.SundayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Monday"), 
            Hours = (int)_settingsManager.MondayLimit.TotalHours, 
            Minutes = _settingsManager.MondayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Tuesday"), 
            Hours = (int)_settingsManager.TuesdayLimit.TotalHours, 
            Minutes = _settingsManager.TuesdayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Wednesday"), 
            Hours = (int)_settingsManager.WednesdayLimit.TotalHours, 
            Minutes = _settingsManager.WednesdayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Thursday"), 
            Hours = (int)_settingsManager.ThursdayLimit.TotalHours, 
            Minutes = _settingsManager.ThursdayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Friday"), 
            Hours = (int)_settingsManager.FridayLimit.TotalHours, 
            Minutes = _settingsManager.FridayLimit.Minutes 
        });
        DayLimits.Add(new DayLimitItem 
        { 
            DayName = LanguageManager.GetString("Saturday"), 
            Hours = (int)_settingsManager.SaturdayLimit.TotalHours, 
            Minutes = _settingsManager.SaturdayLimit.Minutes 
        });

        SelectedLanguageIndex = (int)_settingsManager.Language;
        EnablePasswordLock = _settingsManager.EnablePasswordLock;
    }

    private bool CanExecuteOK(object? parameter)
    {
        return _isPasswordVerified && !_isClosing;
    }

    private bool CanExecuteApplyToAll(object? parameter)
    {
        return DayLimits.Count > 0 && _isPasswordVerified && !_isClosing;
    }

    private void ExecuteOK(object? parameter)
    {
        if (_isClosing) return;

        SaveSettings();
        StatusMessage = LanguageManager.GetString("SettingsSaved");
        _isClosing = true;
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteCancel(object? parameter)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ExecuteChangePassword(object? parameter)
    {
        RequestChangePassword?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteApplyToAll(object? parameter)
    {
        if (DayLimits.Count > 0)
        {
            int firstDayMinutes = DayLimits[0].Hours * 60 + DayLimits[0].Minutes;
            foreach (var day in DayLimits)
            {
                day.Hours = firstDayMinutes / 60;
                day.Minutes = firstDayMinutes % 60;
            }
            StatusMessage = LanguageManager.GetString("AppliedToAllDays");
        }
    }

    private void SaveSettings()
    {
        if (DayLimits.Count >= 7)
        {
            _settingsManager.SundayLimit = TimeSpan.FromMinutes(DayLimits[0].Hours * 60 + DayLimits[0].Minutes);
            _settingsManager.MondayLimit = TimeSpan.FromMinutes(DayLimits[1].Hours * 60 + DayLimits[1].Minutes);
            _settingsManager.TuesdayLimit = TimeSpan.FromMinutes(DayLimits[2].Hours * 60 + DayLimits[2].Minutes);
            _settingsManager.WednesdayLimit = TimeSpan.FromMinutes(DayLimits[3].Hours * 60 + DayLimits[3].Minutes);
            _settingsManager.ThursdayLimit = TimeSpan.FromMinutes(DayLimits[4].Hours * 60 + DayLimits[4].Minutes);
            _settingsManager.FridayLimit = TimeSpan.FromMinutes(DayLimits[5].Hours * 60 + DayLimits[5].Minutes);
            _settingsManager.SaturdayLimit = TimeSpan.FromMinutes(DayLimits[6].Hours * 60 + DayLimits[6].Minutes);
        }

        _settingsManager.Language = (Language)SelectedLanguageIndex;
        _settingsManager.EnablePasswordLock = EnablePasswordLock;
        _settingsManager.SaveSettings();
    }
}
