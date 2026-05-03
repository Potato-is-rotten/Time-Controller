using System.Windows.Input;

namespace ScreenTimeController.Wpf.ViewModels;

public class UnlockViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;
    private readonly TimeTracker _timeTracker;
    private string _password = "";
    private string _statusMessage = "";
    private int _attemptsLeft = 3;
    private bool _isClosing;
    private bool _showingBonusOptions;
    private int _selectedBonusTimeIndex = 0;
    private TimeSpan _bonusTimeAdded;

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool ShowingBonusOptions
    {
        get => _showingBonusOptions;
        set
        {
            if (SetProperty(ref _showingBonusOptions, value))
            {
                OnPropertyChanged(nameof(ShowingPasswordPanel));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool ShowingPasswordPanel => !ShowingBonusOptions;

    public int SelectedBonusTimeIndex
    {
        get => _selectedBonusTimeIndex;
        set => SetProperty(ref _selectedBonusTimeIndex, value);
    }

    public bool IsPasswordCorrect { get; private set; }
    public TimeSpan BonusTimeAdded => _bonusTimeAdded;

    public List<string> BonusTimeOptions { get; }

    public ICommand UnlockCommand { get; }
    public ICommand AddTimeCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? UnlockSuccessful;
    public event EventHandler? Cancelled;

    public UnlockViewModel(SettingsManager settingsManager, TimeTracker timeTracker)
    {
        _settingsManager = settingsManager;
        _timeTracker = timeTracker;
        
        BonusTimeOptions = new List<string>
        {
            LanguageManager.GetString("5Minutes"),
            LanguageManager.GetString("10Minutes"),
            LanguageManager.GetString("15Minutes"),
            LanguageManager.GetString("30Minutes"),
            LanguageManager.GetString("1Hour")
        };

        UnlockCommand = new RelayCommand(ExecuteUnlock, CanExecuteUnlock);
        AddTimeCommand = new RelayCommand(ExecuteAddTime, CanExecuteAddTime);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    private bool CanExecuteUnlock(object? parameter)
    {
        return !_isClosing && !ShowingBonusOptions;
    }

    private bool CanExecuteAddTime(object? parameter)
    {
        return !_isClosing && ShowingBonusOptions;
    }

    private void ExecuteUnlock(object? parameter)
    {
        if (_isClosing) return;

        if (_settingsManager.HasPassword())
        {
            if (_settingsManager.VerifyPassword(Password))
            {
                ShowingBonusOptions = true;
                return;
            }

            _attemptsLeft--;
            if (_attemptsLeft > 0)
            {
                StatusMessage = string.Format(LanguageManager.GetString("IncorrectPassword"), _attemptsLeft);
                Password = "";
            }
            else
            {
                StatusMessage = LanguageManager.GetString("TooManyAttempts");
                _isClosing = true;
                IsPasswordCorrect = false;
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            ShowingBonusOptions = true;
        }
    }

    private void ExecuteAddTime(object? parameter)
    {
        if (_isClosing) return;

        int minutes = SelectedBonusTimeIndex switch
        {
            0 => 5,
            1 => 10,
            2 => 15,
            3 => 30,
            4 => 60,
            _ => 5
        };

        _bonusTimeAdded = TimeSpan.FromMinutes(minutes);
        _timeTracker.AddBonusTime(_bonusTimeAdded);
        IsPasswordCorrect = true;
        _isClosing = true;
        
        StatusMessage = string.Format(LanguageManager.GetString("BonusTimeAdded"), minutes);
        UnlockSuccessful?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteCancel(object? parameter)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            IsPasswordCorrect = false;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
