using Centraly.Api.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Centraly.Desktop;

public static class DesktopDependencies
{
    public static IServiceCollection AddCentralyDesktop(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(op => op.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddCentralyCoreServices();

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<INotificationPublisher, InProcessNotificationPublisher>();

        services.AddSingleton<ICurrentUser, CurrentUser>();
        services.AddSingleton<IServiceRunner, ServiceRunner>();

        services.AddHostedService<HourlyBackgroundJobs>();

        return services;
    }
}
