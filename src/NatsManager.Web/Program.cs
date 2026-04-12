using FluentValidation;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Application.Modules.Auth.Services;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Auth;
using NatsManager.Infrastructure.Configuration;
using NatsManager.Infrastructure.Nats;
using NatsManager.Infrastructure.Persistence;
using NatsManager.Web.BackgroundServices;
using NatsManager.Web.Endpoints;
using NatsManager.Web.Middleware;
using NatsManager.Web.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NatsManager")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}", formatProvider: System.Globalization.CultureInfo.InvariantCulture)
        .WriteTo.File("logs/natsmanager-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            formatProvider: System.Globalization.CultureInfo.InvariantCulture,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}"));

builder.AddServiceDefaults();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=natsmanager.db"));

// Use cases + FluentValidation
var applicationAssembly = typeof(NatsManager.Application.Common.PaginatedQuery<object>).Assembly;
builder.Services.AddUseCases(applicationAssembly);
builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// Infrastructure services
builder.Services.AddSingleton<IEnvironmentConnectionResolver, NatsManager.Infrastructure.Nats.EnvironmentConnectionResolver>();
builder.Services.AddSingleton<INatsConnectionFactory, NatsConnectionFactory>();
builder.Services.AddScoped<IAuditEventRepository, AuditEventRepository>();
builder.Services.AddScoped<IAuthorizationService, NatsManager.Infrastructure.Auth.AuthorizationService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<NatsManager.Application.Modules.Auth.Ports.IPasswordHasher>(sp => sp.GetRequiredService<PasswordHasher>());
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<HttpAuditContext>();
builder.Services.AddScoped<IAuditContext>(sp => sp.GetRequiredService<HttpAuditContext>());
builder.Services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
builder.Services.AddScoped<NatsManager.Application.Modules.Auth.Ports.IUserRepository, NatsManager.Infrastructure.Persistence.UserRepository>();
builder.Services.AddScoped<NatsManager.Application.Modules.Search.Ports.IBookmarkRepository, NatsManager.Infrastructure.Persistence.BookmarkRepository>();
builder.Services.AddScoped<NatsManager.Application.Modules.Search.Ports.IUserPreferenceRepository, NatsManager.Infrastructure.Persistence.UserPreferenceRepository>();
builder.Services.AddSingleton<INatsHealthChecker, NatsHealthChecker>();
builder.Services.AddSingleton<NatsManager.Application.Modules.JetStream.Ports.IJetStreamAdapter, NatsManager.Infrastructure.Nats.JetStreamAdapter>();
builder.Services.AddSingleton<NatsManager.Application.Modules.JetStream.Ports.IJetStreamWriteAdapter>(sp =>
    (NatsManager.Infrastructure.Nats.JetStreamAdapter)sp.GetRequiredService<NatsManager.Application.Modules.JetStream.Ports.IJetStreamAdapter>());
builder.Services.AddSingleton<NatsManager.Application.Modules.KeyValue.Ports.IKvStoreAdapter, NatsManager.Infrastructure.Nats.KvStoreAdapter>();
builder.Services.AddSingleton<NatsManager.Application.Modules.Services.Ports.IServiceDiscoveryAdapter, NatsManager.Infrastructure.Nats.ServiceDiscoveryAdapter>();
builder.Services.AddSingleton<NatsManager.Application.Modules.ObjectStore.Ports.IObjectStoreAdapter, NatsManager.Infrastructure.Nats.ObjectStoreAdapter>();
builder.Services.AddSingleton<NatsManager.Application.Modules.CoreNats.Ports.ICoreNatsAdapter, NatsManager.Infrastructure.Nats.CoreNatsAdapter>();

builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

builder.Services.AddSingleton<ICredentialEncryptionService>(_ =>
{
    var encryptionKey = builder.Configuration["Encryption:Key"];
    if (string.IsNullOrWhiteSpace(encryptionKey))
    {
        throw new InvalidOperationException(
            "Encryption:Key must be configured as a base64-encoded 32-byte key.");
    }

    try
    {
        return new CredentialEncryptionService(Convert.FromBase64String(encryptionKey));
    }
    catch (Exception ex) when (ex is FormatException or ArgumentException)
    {
        throw new InvalidOperationException(
            "Encryption:Key must be a valid base64-encoded 32-byte key.",
            ex);
    }
});

// Authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});
builder.Services.AddAuthentication(SessionAuthHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SessionAuthHandler>(SessionAuthHandler.SchemeName, null);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(Role.PredefinedNames.Administrator));

    options.AddPolicy(AuthorizationPolicyNames.AuditRead, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(Role.PredefinedNames.Administrator, Role.PredefinedNames.Auditor));

    options.AddPolicy(AuthorizationPolicyNames.OperatorAccess, policy =>
        policy.RequireAuthenticatedUser()
            .RequireRole(Role.PredefinedNames.Administrator, Role.PredefinedNames.Operator));
});

// Error handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Background services
builder.Services.AddHostedService<EnvironmentHealthPoller>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseAntiforgery();
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsGet(context.Request.Method) && context.Request.Path.StartsWithSegments("/api"))
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var tokens = antiforgery.GetAndStoreTokens(context);

            if (!string.IsNullOrEmpty(tokens.RequestToken))
            {
                context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    SameSite = SameSiteMode.Strict,
                    Secure = context.Request.IsHttps
                });
            }
        }

        await next(context);
    });
}

app.UseMiddleware<AuditContextMiddleware>();
app.UseMiddleware<DataFreshnessMiddleware>();

app.MapDefaultEndpoints();

// Map API endpoints
app.MapEnvironmentEndpoints();
app.MapJetStreamEndpoints();
app.MapKvEndpoints();
app.MapDashboardEndpoints();
app.MapServiceEndpoints();
app.MapObjectStoreEndpoints();
app.MapCoreNatsEndpoints();
app.MapAuthEndpoints();
app.MapAccessControlEndpoints();
app.MapAuditEndpoints();
app.MapSearchEndpoints();

app.Run();

public partial class Program { }
