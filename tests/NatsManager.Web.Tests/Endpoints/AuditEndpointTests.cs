using System.Net;
using Shouldly;
using NSubstitute;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Domain.Modules.Audit;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class AuditEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public AuditEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient(Role.PredefinedNames.Auditor);

        // Set up default return for audit event queries
        _factory.AuditEventRepository.GetPagedAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<Guid?>(), Arg.Any<NatsManager.Domain.Modules.Common.ActionType?>(),
            Arg.Any<NatsManager.Domain.Modules.Common.ResourceType?>(), Arg.Any<Guid?>(),
            Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<NatsManager.Domain.Modules.Common.AuditSource?>(), Arg.Any<CancellationToken>())
            .Returns((new List<AuditEvent>() as IReadOnlyList<AuditEvent>, 0));
    }

    [Fact]
    public async Task GetAuditEvents_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/audit/events");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditEvents_WithFilters_ShouldReturn200()
    {
        var response = await _client.GetAsync("/api/audit/events?page=1&pageSize=10&actionType=Create");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAuditEvents_WhenAnonymous_ShouldReturn401()
    {
        using var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/audit/events");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAuditEvents_WhenAuthenticatedWithoutAuditRole_ShouldReturn403()
    {
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Operator);

        var response = await client.GetAsync("/api/audit/events");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
