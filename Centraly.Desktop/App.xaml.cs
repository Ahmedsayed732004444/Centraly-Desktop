using Centraly.Desktop.ViewModels;
using Centraly.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Windows.Threading;

namespace Centraly.Desktop;

public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"حدث خطأ غير متوقع:\n{args.Exception.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddCentralyDesktop(context.Configuration);
                services.AddTransient<LoginViewModel>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<MainWindow>();
                services.AddTransient<PosViewModel>();
                services.AddTransient<PosPage>();
            })
            .Build();

        await AppHost.StartAsync();

        using (var scope = AppHost.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"تعذر الاتصال بقاعدة البيانات المحلية أو تحديثها:\n{ex.Message}",
                    "خطأ في قاعدة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }
        }

        var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost.StopAsync();
        AppHost.Dispose();
        base.OnExit(e);
    }
}
