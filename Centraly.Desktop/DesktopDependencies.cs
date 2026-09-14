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

        // LocalDB can be mid-cold-start on the first connection of a session (it auto-shuts
        // down after ~15 min idle) - retry instead of failing outright on that one hiccup.
        services.AddDbContext<ApplicationDbContext>(op => op.UseSqlServer(
            connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

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
