using System.Windows.Input;

namespace ScreenTimeController.Wpf.ViewModels;

public class ChangePasswordViewModel : ViewModelBase
{
    private readonly SettingsManager _settingsManager;
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string _statusMessage = "";
    private bool _isClosing;

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand OKCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RemovePasswordCommand { get; }

    public event EventHandler? PasswordChanged;
    public event EventHandler? Cancelled;
    public event EventHandler? RequestRemovePassword;

    public ChangePasswordViewModel(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        OKCommand = new RelayCommand(ExecuteOK, CanExecuteOK);
        CancelCommand = new RelayCommand(ExecuteCancel);
        RemovePasswordCommand = new RelayCommand(ExecuteRemovePassword);
    }

    private bool CanExecuteOK(object? parameter)
    {
        return !_isClosing;
    }

    private void ExecuteOK(object? parameter)
    {
        if (_isClosing) return;

        if (string.IsNullOrEmpty(NewPassword))
        {
            StatusMessage = LanguageManager.GetString("PasswordEmpty");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = LanguageManager.GetString("PasswordMismatch");
            return;
        }

        _settingsManager.SetPassword(NewPassword);
        _settingsManager.SaveSettings();
        StatusMessage = LanguageManager.GetString("PasswordChanged");
        _isClosing = true;
        PasswordChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteCancel(object? parameter)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ExecuteRemovePassword(object? parameter)
    {
        RequestRemovePassword?.Invoke(this, EventArgs.Empty);
    }

    public void RemovePassword()
    {
        _settingsManager.SetPassword("");
        _settingsManager.SaveSettings();
        StatusMessage = LanguageManager.GetString("PasswordRemoved");
        _isClosing = true;
        PasswordChanged?.Invoke(this, EventArgs.Empty);
    }
}
