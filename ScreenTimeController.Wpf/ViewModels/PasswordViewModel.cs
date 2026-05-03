using System.Windows.Input;

namespace ScreenTimeController.Wpf.ViewModels;

public class PasswordViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;
    private readonly LoginAttemptManager _loginAttemptManager;
    private string _password = "";
    private string _lockStatus = "";
    private bool _isLocked;
    private bool _isPasswordCorrect;
    private bool _isClosing;

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string LockStatus
    {
        get => _lockStatus;
        set => SetProperty(ref _lockStatus, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (SetProperty(ref _isLocked, value))
            {
                OnPropertyChanged(nameof(CanEnterPassword));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanEnterPassword => !IsLocked;

    public bool IsPasswordCorrect => _isPasswordCorrect;

    public ICommand OKCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler? PasswordVerified;
    public event EventHandler? Cancelled;

    public PasswordViewModel(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        _loginAttemptManager = new LoginAttemptManager();
        OKCommand = new RelayCommand(ExecuteOK, CanExecuteOK);
        CancelCommand = new RelayCommand(ExecuteCancel);
        CheckLockStatus();
    }

    private void CheckLockStatus()
    {
        if (!_settingsManager.EnablePasswordLock)
        {
            LockStatus = "";
            IsLocked = false;
            return;
        }

        if (_loginAttemptManager.IsLocked)
        {
            LockStatus = string.Format(LanguageManager.GetString("LockedUntil"),
                DateTime.Today.AddDays(1).ToString("HH:mm"));
            IsLocked = true;
        }
        else
        {
            int attempts = _loginAttemptManager.FailedAttempts;
            if (attempts > 0)
            {
                LockStatus = string.Format(LanguageManager.GetString("FailedAttempts"), attempts);
            }
            else
            {
                LockStatus = "";
            }
            IsLocked = false;
        }
    }

    private bool CanExecuteOK(object? parameter)
    {
        return !IsLocked && !_isClosing;
    }

    private void ExecuteOK(object? parameter)
    {
        if (_isClosing) return;

        if (_loginAttemptManager.IsLocked)
        {
            LockStatus = LanguageManager.GetString("AccountLocked");
            return;
        }

        if (_settingsManager.VerifyPassword(Password))
        {
            _isPasswordCorrect = true;
            _loginAttemptManager.ResetAttempts();
            _isClosing = true;
            PasswordVerified?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_settingsManager.EnablePasswordLock)
        {
            _loginAttemptManager.RecordFailedAttempt();

            if (_loginAttemptManager.IsLocked)
            {
                LockStatus = LanguageManager.GetString("AccountLockedUntilTomorrow");
                IsLocked = true;
                _isClosing = true;
                Cancelled?.Invoke(this, EventArgs.Empty);
                return;
            }

            int attemptsLeft = 5 - _loginAttemptManager.FailedAttempts;
            if (attemptsLeft > 0)
            {
                LockStatus = string.Format(LanguageManager.GetString("IncorrectPassword"), attemptsLeft);
            }
            else
            {
                LockStatus = LanguageManager.GetString("AccountLockedUntilTomorrow");
                IsLocked = true;
                _isClosing = true;
                Cancelled?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            LockStatus = LanguageManager.GetString("PasswordIncorrect");
        }

        Password = "";
        CheckLockStatus();
    }

    private void ExecuteCancel(object? parameter)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}
