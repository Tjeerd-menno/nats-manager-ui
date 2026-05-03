using FluentValidation;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Options;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Application.Modules.Auth.Services;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Auth;
using NatsManager.Infrastructure.Configuration;
using NatsManager.Infrastructure.Monitoring;
using NatsManager.Infrastructure.Monitoring.ClusterObservability;
using NatsManager.Infrastructure.Nats;
using NatsManager.Infrastructure.Nats.ClusterObservability;
using NatsManager.Infrastructure.Persistence;
using NatsManager.Infrastructure.Relationships;
using NatsManager.Infrastructure.Relationships.Sources;
using NatsManager.Web.BackgroundServices;
using NatsManager.Web.Configuration;
using NatsManager.Web.Endpoints;
using NatsManager.Web.Hubs;
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

// EF Core (provider selected by Database:Provider — defaults to SQLite)
builder.Services.AddNatsManagerPersistence(builder.Configuration);

// Use cases + FluentValidation
var applicationAssembly = typeof(NatsManager.Application.Common.PaginatedQuery<object>).Assembly;
builder.Services.AddUseCases(applicationAssembly);
builder.Services.AddValidatorsFromAssembly(applicationAssembly);

// Infrastructure services
builder.Services.AddSingleton<IEnvironmentConnectionResolver, NatsManager.Infrastructure.Nats.EnvironmentConnectionResolver>();
builder.Services.AddSingleton<INatsConnectionFactory, NatsConnectionFactory>();
builder.Services.AddScoped<IAuditEventRepository, AuditEventRepository>();
builder.Services.AddScoped<IAuthorizationService, NatsManager.Infrastructure.Auth.AuthorizationService>();
builder.Services.AddSingleton<NatsManager.Application.Modules.Auth.Ports.IPasswordHasher, PasswordHasher>();
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

builder.Services.Configure<CoreNatsMonitoringOptions>(
    builder.Configuration.GetSection(CoreNatsMonitoringOptions.SectionName));
builder.Services.AddHttpClient();

builder.Services.Configure<BootstrapAdminOptions>(
    builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

builder.Services.AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
    .Validate(MonitoringOptions.IsValid, "Monitoring options are invalid. DefaultPollingIntervalSeconds must be 5-300, MaxSnapshotsPerEnvironment must be 1-10000, and HttpTimeoutSeconds must be 1-60.")
    .ValidateOnStart();

builder.Services.AddOptions<ObjectStoreUploadOptions>()
    .Bind(builder.Configuration.GetSection(ObjectStoreUploadOptions.SectionName))
    .Validate(ObjectStoreUploadOptions.IsValid, "ObjectStore options are invalid. MaxUploadBytes must be between 1 and 2147483647.")
    .ValidateOnStart();

builder.Services.AddHttpClient("NatsMonitoring", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MonitoringOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
});

builder.Services.AddHttpClient("NatsClusterMonitoring", (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MonitoringOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(opts.ClusterEndpointTimeoutSeconds);
    client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<IMonitoringAdapter, NatsMonitoringHttpAdapter>();
builder.Services.AddSingleton<IMonitoringMetricsStore, MonitoringMetricsStore>();

// Cluster Observability
builder.Services.AddSingleton<IClusterMonitoringAdapter, NatsClusterMonitoringHttpAdapter>();
builder.Services.AddSingleton<IClusterObservationStore, ClusterObservationStore>();

// Relationships
builder.Services.AddSingleton<IRelationshipSource, JetStreamRelationshipSource>();
builder.Services.AddSingleton<IRelationshipSource, KeyValueRelationshipSource>();
builder.Services.AddSingleton<IRelationshipSource, ObjectStoreRelationshipSource>();
builder.Services.AddSingleton<IRelationshipSource, ServicesRelationshipSource>();
builder.Services.AddSingleton<IRelationshipSource, CoreNatsRelationshipSource>();
builder.Services.AddSingleton<IRelationshipSource, MonitoringRelationshipSource>();
builder.Services.AddSingleton<IFocalResourceResolver, FocalResourceResolver>();
builder.Services.AddSingleton<RelationshipProjectionService>();
builder.Services.AddScoped<GetRelationshipMapQueryHandler>();

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

// CORS origins are read early so that the session cookie policy can be aligned.
// When the SPA is served from a different origin than the API, cookies must use
// SameSite=None (+ Secure) so that browsers will include them in cross-site
// requests. With no cross-origin origins configured the stricter SameSite=Strict
// default is kept.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

var crossOriginEnabled = allowedOrigins.Length > 0;

// Session — sliding 30-minute idle window. Abandoned browser tabs or stolen session
// cookies are invalidated far sooner than the previous 8-hour window.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // When CORS cross-origin access is enabled the cookie must be SameSite=None
    // so browsers will send it on cross-site requests. Secure is required by the
    // SameSite=None specification.
    options.Cookie.SecurePolicy = crossOriginEnabled
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = crossOriginEnabled
        ? SameSiteMode.None
        : SameSiteMode.Strict;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, EnvironmentScopedRoleAuthorizationHandler>();
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
            .AddRequirements(new EnvironmentScopedRoleRequirement(
                Role.PredefinedNames.Administrator,
                Role.PredefinedNames.Operator)));
});

// CORS — by default the frontend is served from the same origin as the API
// and CORS is not required. For deployments where the SPA is hosted separately
// (e.g. a CDN), populate `Cors:AllowedOrigins` with the explicit list of trusted
// origins. The policy is opt-in: with no allowed origins configured the
// middleware is effectively a no-op for cross-origin requests.
// NOTE: enabling cross-origin origins also sets the session cookie to
// SameSite=None + Secure (see session configuration above) so that browsers
// will include it in cross-site requests.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            // Deny-by-default: no origins allowed means no CORS response headers
            // will be emitted, leaving browsers' same-origin policy in force.
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate limiting — protects authentication and other sensitive endpoints from
// brute-force and abusive traffic. Keyed by authenticated user when available,
// otherwise by remote IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict limiter for the login endpoint: 5 attempts per minute per client IP.
    // NOTE: Partitions by the socket-level remote IP. If the app is deployed behind
    // a reverse proxy, configure `ForwardedHeadersOptions` (with a trusted
    // `KnownProxies`/`KnownNetworks` set) so that `RemoteIpAddress` reflects the
    // real client; otherwise all traffic will share a single partition keyed on the
    // proxy's IP, effectively disabling the limiter.
    options.AddPolicy(RateLimitPolicyNames.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    // Global fallback: generous per-client sliding window to limit abusive clients
    // without interfering with normal interactive use.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity.Name}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
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
builder.Services.AddHostedService<MonitoringPoller>();
builder.Services.AddHostedService<ClusterObservationPoller>();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Apply baseline security headers before any handler so every response
// (including error responses) is covered.
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Enable rate limiting after authentication so that authenticated users
// get their own partition.
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseAntiforgery();
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

            if (!HttpMethods.IsGet(context.Request.Method)
                && !HttpMethods.IsHead(context.Request.Method)
                && !HttpMethods.IsOptions(context.Request.Method)
                && !HttpMethods.IsTrace(context.Request.Method)
                && !context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    await Results.Problem(
                        title: "Invalid antiforgery token",
                        detail: "Unsafe API requests require a valid X-XSRF-TOKEN header.",
                        statusCode: StatusCodes.Status400BadRequest)
                        .ExecuteAsync(context);
                    return;
                }
            }
            else if (HttpMethods.IsGet(context.Request.Method))
            {
                var tokens = antiforgery.GetAndStoreTokens(context);

                if (!string.IsNullOrEmpty(tokens.RequestToken))
                {
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                    {
                        HttpOnly = false,
                        IsEssential = true,
                        SameSite = crossOriginEnabled ? SameSiteMode.None : SameSiteMode.Strict,
                        Secure = crossOriginEnabled || context.Request.IsHttps
                    });
                }
            }
        }

        await next(context);
    });
}

app.UseMiddleware<AuditContextMiddleware>();
app.UseMiddleware<DataFreshnessMiddleware>();

app.MapDefaultEndpoints();

// Map SignalR hubs
app.MapHub<MonitoringHub>("/hubs/monitoring");

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
app.MapMonitoringEndpoints();
app.MapRelationshipMapEndpoints();

app.MapFallback(async (HttpContext context, IWebHostEnvironment environment) =>
{
    if (context.Request.Path.StartsWithSegments("/api")
        || context.Request.Path.StartsWithSegments("/hubs"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexFile = environment.WebRootFileProvider.GetFileInfo("index.html");
    if (!indexFile.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength = indexFile.Length;
    await using var stream = indexFile.CreateReadStream();
    await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();

public partial class Program { }
