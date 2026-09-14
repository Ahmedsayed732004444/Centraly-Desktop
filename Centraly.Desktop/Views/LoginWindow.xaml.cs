using Centraly.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace Centraly.Desktop.Views;

public partial class LoginWindow : FluentWindow
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        // PasswordBox.Password isn't a DependencyProperty (by design, to discourage
        // binding secrets into the view model's string state) - sync it by hand instead.
        PasswordBox.PasswordChanged += (_, _) => _viewModel.Password = PasswordBox.Password;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
    }

    private void OnLoginSucceeded()
    {
        var mainWindow = App.AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        Close();
    }
}
