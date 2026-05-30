using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.Permissions.Services;
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
using NatsManager.Web.Hubs;
using NatsManager.Web.Middleware;
using NatsManager.Web.Security;
using FluentValidation;

namespace NatsManager.Web.Configuration;

/// <summary>
/// Composition-root extension methods that group the application's service
/// registrations into cohesive units, keeping <c>Program.cs</c> readable.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    public static string[] GetAllowedCorsOrigins(this IConfiguration configuration) =>
        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    /// <summary>Registers persistence, application use cases, infrastructure adapters,
    /// monitoring, relationships, permissions, web security and hosting services.</summary>
    public static WebApplicationBuilder AddNatsManagerServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddPersistenceLayer(builder.Configuration)
            .AddApplicationLayer()
            .AddInfrastructureAdapters(builder.Configuration)
            .AddMonitoringServices(builder.Configuration)
            .AddRelationshipServices()
            .AddPermissionServices(builder.Configuration)
            .AddCredentialEncryption(builder.Configuration)
            .AddWebSecurity(builder.Configuration)
            .AddWebHosting(builder.Configuration);

        return builder;
    }

    private static IServiceCollection AddPersistenceLayer(this IServiceCollection services, ConfigurationManager configuration)
    {
        // EF Core (provider selected by Database:Provider — defaults to SQLite)
        services.AddNatsManagerPersistence(configuration);
        return services;
    }

    private static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        // Use cases + FluentValidation
        var applicationAssembly = typeof(PaginatedQuery<object>).Assembly;
        services.AddUseCases(applicationAssembly);
        services.AddValidatorsFromAssembly(applicationAssembly);
        return services;
    }

    private static IServiceCollection AddInfrastructureAdapters(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSingleton<IEnvironmentConnectionResolver, EnvironmentConnectionResolver>();
        services.AddSingleton<INatsConnectionFactory, NatsConnectionFactory>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<NatsManager.Application.Modules.Auth.Services.IAuthorizationService, NatsManager.Infrastructure.Auth.AuthorizationService>();
        services.AddSingleton<NatsManager.Application.Modules.Auth.Ports.IPasswordHasher, PasswordHasher>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<HttpAuditContext>();
        services.AddScoped<IAuditContext>(sp => sp.GetRequiredService<HttpAuditContext>());
        services.AddScoped<IEnvironmentRepository, EnvironmentRepository>();
        services.AddScoped<NatsManager.Application.Modules.Auth.Ports.IUserRepository, UserRepository>();
        services.AddScoped<NatsManager.Application.Modules.Search.Ports.IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<NatsManager.Application.Modules.Search.Ports.IUserPreferenceRepository, UserPreferenceRepository>();
        services.AddSingleton<INatsHealthChecker, NatsHealthChecker>();
        services.AddSingleton<NatsManager.Application.Modules.JetStream.Ports.IJetStreamAdapter, JetStreamAdapter>();
        services.AddSingleton<NatsManager.Application.Modules.JetStream.Ports.IJetStreamWriteAdapter>(sp =>
            (JetStreamAdapter)sp.GetRequiredService<NatsManager.Application.Modules.JetStream.Ports.IJetStreamAdapter>());
        services.AddSingleton<NatsManager.Application.Modules.KeyValue.Ports.IKvStoreAdapter, KvStoreAdapter>();
        services.AddSingleton<NatsManager.Application.Modules.Services.Ports.IServiceDiscoveryAdapter, ServiceDiscoveryAdapter>();
        services.AddSingleton<NatsManager.Application.Modules.ObjectStore.Ports.IObjectStoreAdapter, ObjectStoreAdapter>();
        services.AddSingleton<NatsManager.Application.Modules.CoreNats.Ports.ICoreNatsAdapter, CoreNatsAdapter>();

        services.Configure<CoreNatsMonitoringOptions>(
            configuration.GetSection(CoreNatsMonitoringOptions.SectionName));
        services.AddHttpClient();

        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName));

        return services;
    }

    private static IServiceCollection AddMonitoringServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .Validate(MonitoringOptions.IsValid, "Monitoring options are invalid. DefaultPollingIntervalSeconds must be 5-300, MaxSnapshotsPerEnvironment must be 1-10000, and HttpTimeoutSeconds must be 1-60.")
            .ValidateOnStart();

        services.AddHttpClient("NatsMonitoring", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<MonitoringOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.HttpTimeoutSeconds);
            client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        });

        services.AddHttpClient("NatsClusterMonitoring", (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<MonitoringOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.ClusterEndpointTimeoutSeconds);
            client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        });

        services.AddSignalR();
        services.AddSingleton<IMonitoringAdapter, NatsMonitoringHttpAdapter>();
        services.AddSingleton<IMonitoringMetricsStore, MonitoringMetricsStore>();

        // Cluster Observability
        services.AddScoped<IClusterMonitoringAdapter, NatsClusterMonitoringHttpAdapter>();
        services.AddSingleton<IClusterObservationStore, ClusterObservationStore>();

        return services;
    }

    private static IServiceCollection AddRelationshipServices(this IServiceCollection services)
    {
        services.AddSingleton<IRelationshipSource, JetStreamRelationshipSource>();
        services.AddSingleton<IRelationshipSource, KeyValueRelationshipSource>();
        services.AddSingleton<IRelationshipSource, ObjectStoreRelationshipSource>();
        services.AddSingleton<IRelationshipSource, ServicesRelationshipSource>();
        services.AddSingleton<IRelationshipSource, CoreNatsRelationshipSource>();
        services.AddSingleton<IRelationshipSource, MonitoringRelationshipSource>();
        services.AddSingleton<IFocalResourceResolver, FocalResourceResolver>();
        services.AddSingleton<RelationshipProjectionService>();
        services.AddScoped<GetRelationshipMapQueryHandler>();
        services.AddScoped<GetRelationshipNodeQueryHandler>();
        return services;
    }

    private static IServiceCollection AddPermissionServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOptions<PermissionManifestOptions>()
            .Bind(configuration.GetSection(PermissionManifestOptions.SectionName))
            .Validate(PermissionManifestOptions.IsValid, "PermissionManifest options are invalid. ExposureMode must be Public or Restricted, ApplicationId and ApplicationName are required, and RestrictedAccessKeyHash must be a SHA-256 hash when restricted.")
            .ValidateOnStart();

        services.AddSingleton<IPermissionManifestValidator, PermissionManifestValidator>();
        services.AddSingleton<IPermissionManifestRegistry>(_ => PermissionManifestRegistry.CreateDefault());
        services.AddSingleton<IPermissionManifestPublisher, PermissionManifestPublisher>();
        return services;
    }

    private static IServiceCollection AddCredentialEncryption(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSingleton<ICredentialEncryptionService>(_ =>
        {
            var encryptionKey = configuration["Encryption:Key"];
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
        return services;
    }

    private static IServiceCollection AddWebSecurity(this IServiceCollection services, ConfigurationManager configuration)
    {
        var allowedOrigins = configuration.GetAllowedCorsOrigins();
        var crossOriginEnabled = allowedOrigins.Length > 0;

        // Session — sliding 30-minute idle window. Abandoned browser tabs or stolen session
        // cookies are invalidated far sooner than the previous 8-hour window.
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
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
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
        });
        services.AddHttpContextAccessor();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, EnvironmentScopedRoleAuthorizationHandler>();
        services.AddAuthentication(SessionAuthHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, SessionAuthHandler>(SessionAuthHandler.SchemeName, null);
        services.AddAuthorization(options =>
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

        // CORS — opt-in. With no allowed origins configured the middleware is a no-op
        // for cross-origin requests (deny-by-default).
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Length == 0)
                {
                    return;
                }

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddRateLimiterPolicies();

        return services;
    }

    private static IServiceCollection AddRateLimiterPolicies(this IServiceCollection services)
    {
        // Rate limiting — protects authentication and other sensitive endpoints from
        // brute-force and abusive traffic. Keyed by authenticated user when available,
        // otherwise by remote IP.
        services.AddRateLimiter(options =>
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

        return services;
    }

    private static IServiceCollection AddWebHosting(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddOptions<ObjectStoreUploadOptions>()
            .Bind(configuration.GetSection(ObjectStoreUploadOptions.SectionName))
            .Validate(ObjectStoreUploadOptions.IsValid, "ObjectStore options are invalid. MaxUploadBytes and MaxDownloadBytes must be between 1 and 2147483647.")
            .ValidateOnStart();

        // Error handling
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // JSON serialization
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Background services
        services.AddHostedService<EnvironmentHealthPoller>();
        services.AddHostedService<MonitoringPoller>();
        services.AddHostedService<ClusterObservationPoller>();

        return services;
    }
}
