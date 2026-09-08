using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Shouldly;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Web.Hubs;
using NatsManager.Web.Security;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Hubs;

public sealed class MonitoringHubTests
{
    private readonly IEnvironmentRepository _environmentRepository = Substitute.For<IEnvironmentRepository>();
    private readonly IMonitoringMetricsStore _metricsStore = Substitute.For<IMonitoringMetricsStore>();
    private readonly IGroupManager _groups = Substitute.For<IGroupManager>();

    private MonitoringHub CreateHub(ClaimsPrincipal user)
    {
        var context = Substitute.For<HubCallerContext>();
        context.User.Returns(user);
        context.ConnectionId.Returns("conn-1");
        context.ConnectionAborted.Returns(CancellationToken.None);

        return new MonitoringHub(_environmentRepository, _metricsStore)
        {
            Context = context,
            Groups = _groups,
        };
    }

    private static ClaimsPrincipal UserWithScopedRole(string role, Guid environmentId) =>
        new(new ClaimsIdentity([ScopedRoleClaims.Create(role, environmentId)], "Test"));

    private static ClaimsPrincipal UserWithGlobalRole(string role) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Test"));

    private static ClaimsPrincipal AuthenticatedUserWithNoRoles() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], "Test"));

    [Fact]
    public async Task SubscribeToEnvironment_WithScopedReadRole_AddsToGroup()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var hub = CreateHub(UserWithScopedRole(Role.PredefinedNames.ReadOnly, environment.Id));

        await hub.SubscribeToEnvironment(environment.Id.ToString());

        await _groups.Received(1).AddToGroupAsync("conn-1", $"env-{environment.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToEnvironment_AsGlobalAdministrator_AddsToGroup()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        _environmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>()).Returns(environment);
        var hub = CreateHub(UserWithGlobalRole(Role.PredefinedNames.Administrator));

        await hub.SubscribeToEnvironment(environment.Id.ToString());

        await _groups.Received(1).AddToGroupAsync("conn-1", $"env-{environment.Id}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToEnvironment_WithNoRoleForEnvironment_ThrowsAndDoesNotJoinOrProbeExistence()
    {
        var environmentId = Guid.NewGuid();
        var hub = CreateHub(AuthenticatedUserWithNoRoles());

        await Should.ThrowAsync<HubException>(() => hub.SubscribeToEnvironment(environmentId.ToString()));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        // Authorization must run before the existence check so we never disclose whether the env exists.
        await _environmentRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToEnvironment_WhenScopedToDifferentEnvironment_Throws()
    {
        var requestedEnvironmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();
        var hub = CreateHub(UserWithScopedRole(Role.PredefinedNames.ReadOnly, otherEnvironmentId));

        await Should.ThrowAsync<HubException>(() => hub.SubscribeToEnvironment(requestedEnvironmentId.ToString()));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToEnvironment_WhenAuthorizedButEnvironmentMissing_ThrowsAndDoesNotJoin()
    {
        var environmentId = Guid.NewGuid();
        _environmentRepository.GetByIdAsync(environmentId, Arg.Any<CancellationToken>()).Returns((Environment?)null);
        var hub = CreateHub(UserWithScopedRole(Role.PredefinedNames.ReadOnly, environmentId));

        await Should.ThrowAsync<HubException>(() => hub.SubscribeToEnvironment(environmentId.ToString()));

        await _groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubscribeToEnvironment_WithInvalidEnvironmentId_Throws()
    {
        var hub = CreateHub(UserWithGlobalRole(Role.PredefinedNames.Administrator));

        await Should.ThrowAsync<HubException>(() => hub.SubscribeToEnvironment("not-a-guid"));
    }
}
