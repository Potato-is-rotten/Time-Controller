using System.Windows;
using System.Windows.Input;
using ScreenTimeController.Wpf.ViewModels;

namespace ScreenTimeController.Wpf.Views;

public partial class ChangePasswordView : Window
{
    private readonly ChangePasswordViewModel _viewModel;

    public ChangePasswordView(SettingsManager settingsManager)
    {
        InitializeComponent();
        _viewModel = new ChangePasswordViewModel(settingsManager);
        _viewModel.PasswordChanged += OnPasswordChanged;
        _viewModel.Cancelled += OnCancelled;
        _viewModel.RequestRemovePassword += OnRequestRemovePassword;
        DataContext = _viewModel;
        Title = LanguageManager.GetString("ChangePassword");
        
        NewPasswordBox.PasswordChanged += (s, e) => _viewModel.NewPassword = NewPasswordBox.Password;
        ConfirmPasswordBox.PasswordChanged += (s, e) => _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
    }

    private void OnPasswordChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(LanguageManager.GetString("PasswordChanged"), 
                LanguageManager.GetString("Success"), 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            DialogResult = true;
            Close();
        });
    }

    private void OnCancelled(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            DialogResult = false;
            Close();
        });
    }

    private void OnRequestRemovePassword(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var result = MessageBox.Show(LanguageManager.GetString("ConfirmRemovePassword"),
                LanguageManager.GetString("Confirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _viewModel.RemovePassword();
                MessageBox.Show(LanguageManager.GetString("PasswordRemoved"),
                    LanguageManager.GetString("Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.NewPassword = NewPasswordBox.Password;
            _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            _viewModel.OKCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}
