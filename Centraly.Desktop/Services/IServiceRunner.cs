namespace Centraly.Desktop.Services;

// Every Core service (and the DbContext) is registered Scoped, because that's what an
// ASP.NET request scope naturally gives them. WPF has no request scope, so every operation
// against a service must open and dispose its own DI scope here - never resolve a Scoped
// service once and hold onto it for the app's lifetime (stale DbContext, threading bugs).
public interface IServiceRunner
{
    Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> operation)
        where TService : notnull;

    Task RunAsync<TService>(Func<TService, Task> operation)
        where TService : notnull;
}

public class ServiceRunner(IServiceScopeFactory scopeFactory) : IServiceRunner
{
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> operation)
        where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        return await operation(service);
    }

    public async Task RunAsync<TService>(Func<TService, Task> operation)
        where TService : notnull
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TService>();
        await operation(service);
    }
}
