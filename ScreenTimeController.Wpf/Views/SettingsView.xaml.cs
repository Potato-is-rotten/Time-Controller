using System.Windows;
using System.Windows.Input;
using ScreenTimeController.Wpf.ViewModels;

namespace ScreenTimeController.Wpf.Views;

public partial class SettingsView : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsManager _settingsManager;

    public SettingsView(SettingsManager settingsManager)
    {
        InitializeComponent();
        _settingsManager = settingsManager;
        _viewModel = new SettingsViewModel(settingsManager);
        _viewModel.SettingsSaved += OnSettingsSaved;
        _viewModel.Cancelled += OnCancelled;
        _viewModel.RequestChangePassword += OnRequestChangePassword;
        DataContext = _viewModel;
        Title = LanguageManager.GetString("ScreenTimeSettings");
        
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.VerifyPassword())
        {
            var passwordView = new PasswordView(_settingsManager);
            if (passwordView.ShowDialog() == true && passwordView.IsPasswordCorrect)
            {
                _viewModel.SetPasswordVerified();
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(LanguageManager.GetString("SettingsSaved"),
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

    private void OnRequestChangePassword(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var changePasswordView = new ChangePasswordView(_settingsManager);
            changePasswordView.ShowDialog();
        });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}
