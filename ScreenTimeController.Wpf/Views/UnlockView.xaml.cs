using System.Windows;
using System.Windows.Input;
using ScreenTimeController.Wpf.ViewModels;

namespace ScreenTimeController.Wpf.Views;

public partial class UnlockView : Window
{
    private readonly UnlockViewModel _viewModel;

    public bool IsPasswordCorrect => _viewModel.IsPasswordCorrect;
    public TimeSpan BonusTimeAdded => _viewModel.BonusTimeAdded;

    public UnlockView(SettingsManager settingsManager, TimeTracker timeTracker)
    {
        InitializeComponent();
        _viewModel = new UnlockViewModel(settingsManager, timeTracker);
        _viewModel.UnlockSuccessful += OnUnlockSuccessful;
        _viewModel.Cancelled += OnCancelled;
        DataContext = _viewModel;
        Title = LanguageManager.GetString("AppTitle");
        
        PasswordBox.PasswordChanged += (s, e) => _viewModel.Password = PasswordBox.Password;
        
        Loaded += (s, e) =>
        {
            Activate();
            Focus();
            if (!_viewModel.ShowingBonusOptions)
            {
                PasswordBox.Focus();
            }
        };
    }

    private void OnUnlockSuccessful(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(string.Format(LanguageManager.GetString("BonusTimeAdded"), 
                (int)_viewModel.BonusTimeAdded.TotalMinutes),
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

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.Password = PasswordBox.Password;
            if (!_viewModel.ShowingBonusOptions)
            {
                _viewModel.UnlockCommand.Execute(null);
            }
            else
            {
                _viewModel.AddTimeCommand.Execute(null);
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
