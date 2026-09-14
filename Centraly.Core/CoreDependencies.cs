using Centraly.Api.Services;
using Centraly.Api.Services.Implementation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Centraly.Api;

// Registrations shared by every host (Api, Desktop): the actual business-logic wiring,
// with zero ASP.NET-hosting concerns (no controllers, auth scheme, CORS, Hangfire, Swagger -
// each host registers those itself, since they genuinely differ per host).
public static class CoreDependencies
{
    public static IServiceCollection AddCentralyCoreServices(this IServiceCollection services)
    {
        services.AddMapsterConfig();
        services.AddFluentValidationCoreConfig();
        services.AddHybridCache();

        services.Scan(scan => scan
            .FromAssemblyOf<AuthService>()
            .AddClasses(classes => classes
                .InNamespaces("Centraly.Api.Services"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ISalesReturnService, SalesReturnService>();
        services.AddScoped<IFinancePolicyService, FinancePolicyService>();
        services.AddScoped<ITransactionRouterService, TransactionRouterService>();
        services.AddScoped<IOwnerTransactionService, OwnerTransactionService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ILocalAuthService, LocalAuthService>();

        return services;
    }

    private static IServiceCollection AddMapsterConfig(this IServiceCollection services)
    {
        var mappingConfig = TypeAdapterConfig.GlobalSettings;
        mappingConfig.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton<IMapper>(new Mapper(mappingConfig));
        return services;
    }

    private static IServiceCollection AddFluentValidationCoreConfig(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Translates FluentValidation's ~80 built-in rule messages (required, max
        // length, etc.) to Arabic for every validator that doesn't set its own
        // .WithMessage(...). {PropertyName} still resolves to the C# property name
        // (e.g. "Name") since no per-property Arabic display names are registered -
        // a partial improvement over pure English, not full translation.
        ValidatorOptions.Global.LanguageManager.Enabled = true;
        ValidatorOptions.Global.LanguageManager.Culture = new System.Globalization.CultureInfo("ar");

        return services;
    }
}
