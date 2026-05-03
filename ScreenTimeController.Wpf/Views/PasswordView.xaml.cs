using System.Windows;
using System.Windows.Input;
using ScreenTimeController.Wpf.ViewModels;

namespace ScreenTimeController.Wpf.Views;

public partial class PasswordView : Window
{
    private readonly PasswordViewModel _viewModel;

    public bool IsPasswordCorrect => _viewModel.IsPasswordCorrect;

    public PasswordView(SettingsManager settingsManager)
    {
        InitializeComponent();
        _viewModel = new PasswordViewModel(settingsManager);
        _viewModel.PasswordVerified += OnPasswordVerified;
        _viewModel.Cancelled += OnCancelled;
        DataContext = _viewModel;
        Title = LanguageManager.GetString("EnterPassword");
        
        PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
    }

    private void OnPasswordVerified(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_viewModel.CanEnterPassword)
            {
                _viewModel.Password = PasswordBox.Password;
                _viewModel.OKCommand.Execute(null);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }
}
