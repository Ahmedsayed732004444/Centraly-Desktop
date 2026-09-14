namespace Centraly.Desktop.ViewModels;

public partial class LoginViewModel(IServiceRunner runner, ICurrentUser currentUser) : ObservableObject
{
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    public event Action? LoginSucceeded;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "من فضلك أدخل اسم المستخدم وكلمة المرور";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await runner.RunAsync<ILocalAuthService, Result<LocalLoginResult>>(
                auth => auth.LoginAsync(UserName, Password));

            if (result.IsFailure)
            {
                ErrorMessage = result.Error.Description;
                return;
            }

            currentUser.SignIn(result.Value);
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"تعذر تسجيل الدخول: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
