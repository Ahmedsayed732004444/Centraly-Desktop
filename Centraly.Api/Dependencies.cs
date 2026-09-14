using Centraly.Api.Authentication;
using Centraly.Api.Middlewares;
using Centraly.Api.Services;
using Centraly.Api.Services.Abstraction;
using Centraly.Api.Services.Implementation;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Centraly.Api;

public static class Dependencies
{
    public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Match ResultExtensions.ToProblem()'s shape exactly - errors:[code, description] -
                // instead of the framework default { errors: { field: [msgs] } } dictionary, so the
                // frontend has one shape to unwrap regardless of whether a request failed business
                // validation (FluentValidation) or a Result-based business rule.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var description = string.Join("، ", context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors)
                        .Select(e => e.ErrorMessage));

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "بيانات غير صالحة",
                        Extensions = { ["errors"] = new[] { "Validation.Error", description } }
                    };

                    return new BadRequestObjectResult(problemDetails);
                };
            });
        services.AddOpenApi();
        services.AddSignalR();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHybridCache();

        services.AddCors(options =>
            options.AddDefaultPolicy(builder =>
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true)
            )
        );
        services.AddAuthConfig(configuration);
        services
            .AddSwaggerService()
            .AddFluentValidationConfig();

        services.AddCentralyCoreServices();

        services.AddHttpContextAccessor();
        services.AddScoped<IFileStorage, WebFileStorage>();
        services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();


        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(op => op.UseSqlServer(connectionString));
        services.AddBackgroundJobsConfig(configuration);

        return services;
    }

    private static IServiceCollection AddAuthConfig(this IServiceCollection services,
     IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<ArabicIdentityErrorDescriber>();

        services.AddSingleton<IJwtProvider, JwtProvider>();

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtSettings = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>();
        // ── Authentication pipeline ─────────────────────────────────────────
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings!.Key)),
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience
            };

            // ── SignalR JWT from Query String ───────────────────────────────
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        // ── Prevent cookie redirects on API endpoints → return 401/403 ─────
        services.ConfigureApplicationCookie(options =>
        {
            options.Events = new Microsoft.AspNetCore.Authentication.Cookies
                .CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api") ||
                        ctx.Request.Headers["Accept"].ToString()
                           .Contains("application/json"))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api") ||
                        ctx.Request.Headers["Accept"].ToString()
                           .Contains("application/json"))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                }
            };
        });

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = false;
        });

        return services;
    }


    private static IServiceCollection AddSwaggerService(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    // Arabic-message + validator-assembly scan lives in Core (AddCentralyCoreServices) -
    // shared with Desktop. This just wires the ASP.NET MVC auto-validation pipeline on top.
    private static IServiceCollection AddFluentValidationConfig(this IServiceCollection services) =>
        services.AddFluentValidationAutoValidation();
    private static IServiceCollection AddBackgroundJobsConfig(this IServiceCollection services,
    IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer();

        return services;
    }
}



