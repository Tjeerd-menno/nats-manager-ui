using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Application.Modules.Services.Ports;
using NatsManager.Infrastructure.Persistence;

namespace NatsManager.Web.Tests;

public sealed class NatsManagerWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IEnvironmentRepository EnvironmentRepository { get; } = Substitute.For<IEnvironmentRepository>();
    public IJetStreamAdapter JetStreamAdapter { get; } = Substitute.For<IJetStreamAdapter>();
    public IJetStreamWriteAdapter JetStreamWriteAdapter { get; } = Substitute.For<IJetStreamWriteAdapter>();
    public IKvStoreAdapter KvStoreAdapter { get; } = Substitute.For<IKvStoreAdapter>();
    public IObjectStoreAdapter ObjectStoreAdapter { get; } = Substitute.For<IObjectStoreAdapter>();
    public IServiceDiscoveryAdapter ServiceDiscoveryAdapter { get; } = Substitute.For<IServiceDiscoveryAdapter>();
    public ICoreNatsAdapter CoreNatsAdapter { get; } = Substitute.For<ICoreNatsAdapter>();
    public INatsConnectionFactory ConnectionFactory { get; } = Substitute.For<INatsConnectionFactory>();
    public INatsHealthChecker HealthChecker { get; } = Substitute.For<INatsHealthChecker>();
    public ICredentialEncryptionService EncryptionService { get; } = Substitute.For<ICredentialEncryptionService>();
    public IAuditEventRepository AuditEventRepository { get; } = Substitute.For<IAuditEventRepository>();
    public IMonitoringAdapter MonitoringAdapter { get; } = Substitute.For<IMonitoringAdapter>();
    public IMonitoringMetricsStore MonitoringMetricsStore { get; } = Substitute.For<IMonitoringMetricsStore>();
    public IClusterMonitoringAdapter ClusterMonitoringAdapter { get; } = Substitute.For<IClusterMonitoringAdapter>();
    public IClusterObservationStore ClusterObservationStore { get; } = Substitute.For<IClusterObservationStore>();
    public const string BootstrapAdminUsername = "bootstrap-admin";
    public const string BootstrapAdminPassword = "Bootstrap123!";
    public static string EncryptionKey { get; } = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BootstrapAdmin:Username"] = BootstrapAdminUsername,
                ["BootstrapAdmin:Password"] = BootstrapAdminPassword,
                ["Encryption:Key"] = EncryptionKey
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace only the AppDbContext options to use in-memory SQLite (same provider, just different connection)
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in dbDescriptors) services.Remove(d);

            _connection.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // Replace authentication with a test scheme that always succeeds
            services.Configure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = "Test";
                o.DefaultChallengeScheme = "Test";
            });
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Replace NATS adapters with substitutes
            ReplaceService<IEnvironmentRepository>(services, EnvironmentRepository);
            ReplaceService<IJetStreamAdapter>(services, JetStreamAdapter);
            ReplaceService<IJetStreamWriteAdapter>(services, JetStreamWriteAdapter);
            ReplaceService<IKvStoreAdapter>(services, KvStoreAdapter);
            ReplaceService<IObjectStoreAdapter>(services, ObjectStoreAdapter);
            ReplaceService<IServiceDiscoveryAdapter>(services, ServiceDiscoveryAdapter);
            ReplaceService<ICoreNatsAdapter>(services, CoreNatsAdapter);
            ReplaceService<INatsConnectionFactory>(services, ConnectionFactory);
            ReplaceService<INatsHealthChecker>(services, HealthChecker);
            ReplaceService<ICredentialEncryptionService>(services, EncryptionService);
            ReplaceService<IAuditEventRepository>(services, AuditEventRepository);
            ReplaceService<IMonitoringAdapter>(services, MonitoringAdapter);
            ReplaceService<IMonitoringMetricsStore>(services, MonitoringMetricsStore);
            ReplaceService<IClusterMonitoringAdapter>(services, ClusterMonitoringAdapter);
            ReplaceService<IClusterObservationStore>(services, ClusterObservationStore);
        });
    }

    public new HttpClient CreateClient()
        => CreateAuthenticatedClient(Role.PredefinedNames.Administrator);

    public HttpClient CreateAnonymousClient()
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "anonymous");
        return client;
    }

    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "authenticated");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, TestAuthHandler.DefaultUserId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.UsernameHeaderName, BootstrapAdminUsername);
        client.DefaultRequestHeaders.Add(TestAuthHandler.DisplayNameHeaderName, "Administrator");
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.RolesHeaderName,
            string.Join(',', roles.Length == 0 ? [Role.PredefinedNames.Administrator] : roles));
        return client;
    }

    private static void ReplaceService<T>(IServiceCollection services, T substitute) where T : class
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors) services.Remove(d);
        services.AddSingleton(substitute);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    internal const string AuthModeHeaderName = "X-Test-Auth";
    internal const string UserIdHeaderName = "X-Test-UserId";
    internal const string UsernameHeaderName = "X-Test-Username";
    internal const string DisplayNameHeaderName = "X-Test-DisplayName";
    internal const string RolesHeaderName = "X-Test-Roles";
    internal static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(AuthModeHeaderName, out var authMode)
            && StringValues.Equals(authMode, "anonymous"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValues)
            ? userIdValues.ToString()
            : TestAuthHandler.DefaultUserId.ToString();
        var username = Request.Headers.TryGetValue(UsernameHeaderName, out var usernameValues)
            ? usernameValues.ToString()
            : NatsManagerWebAppFactory.BootstrapAdminUsername;
        var displayName = Request.Headers.TryGetValue(DisplayNameHeaderName, out var displayNameValues)
            ? displayNameValues.ToString()
            : "Administrator";
        var roles = Request.Headers.TryGetValue(RolesHeaderName, out var roleValues)
            ? roleValues.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [Role.PredefinedNames.Administrator];

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim("DisplayName", displayName),
        }.Concat(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
