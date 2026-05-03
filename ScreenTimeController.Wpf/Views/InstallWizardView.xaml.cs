using System.Windows;
using ScreenTimeController.Wpf.ViewModels;

namespace ScreenTimeController.Wpf.Views;

public partial class InstallWizardView : Window
{
    private readonly InstallWizardViewModel _viewModel;

    public bool InstallCompleted => _viewModel.InstallCompleted;

    public InstallWizardView()
    {
        InitializeComponent();
        _viewModel = new InstallWizardViewModel();
        _viewModel.InstallFinished += OnInstallFinished;
        _viewModel.SkipRequested += OnSkipRequested;
        DataContext = _viewModel;
        Title = LanguageManager.GetString("AppTitle");
    }

    private void OnInstallFinished(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            DialogResult = true;
            Close();
        });
    }

    private void OnSkipRequested(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            DialogResult = false;
            Close();
        });
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {

    }
}
