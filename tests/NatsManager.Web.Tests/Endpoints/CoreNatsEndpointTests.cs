using System.Net;
using System.Net.Http.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.CoreNats.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class CoreNatsEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public CoreNatsEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_WhenNotFound_ShouldReturn404()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.GetServerInfoAsync(envId, Arg.Any<CancellationToken>())
            .Returns((NatsServerInfo?)null);

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/status");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubjects_WhenMonitoringAvailable_Returns200WithSubjectList()
    {
        var envId = Guid.NewGuid();
        var subjects = new List<NatsSubjectInfo> { new("orders.>", 3) };
        _factory.CoreNatsAdapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new ListSubjectsResult(subjects, IsMonitoringAvailable: true));

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/subjects");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Subjects-Source", out var headerValues).ShouldBeTrue();
        headerValues!.First().ShouldBe("monitoring");
        var body = await response.Content.ReadFromJsonAsync<List<NatsSubjectInfo>>();
        body.ShouldNotBeNull();
        body!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetSubjects_WhenMonitoringUnavailable_Returns200WithEmptyListAndUnavailableHeader()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new ListSubjectsResult([], IsMonitoringAvailable: false));

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/subjects");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Subjects-Source", out var headerValues).ShouldBeTrue();
        headerValues!.First().ShouldBe("unavailable");
        var body = await response.Content.ReadFromJsonAsync<List<NatsSubjectInfo>>();
        body.ShouldNotBeNull();
        body!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PublishMessage_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.PublishAsync(
                envId,
                "test.subject",
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var payload = new { Subject = "test.subject", Payload = "hello" };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishMessage_WithHeadersAndReplyTo_Returns200()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.PublishAsync(
                envId,
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<IReadOnlyDictionary<string, string>?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var payload = new
        {
            Subject = "test.subject",
            Payload = "{\"key\":\"value\"}",
            PayloadFormat = "Json",
            Headers = new Dictionary<string, string> { ["X-Source"] = "test" },
            ReplyTo = "test.reply",
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishMessage_WithInvalidJsonFormat_Returns422()
    {
        var envId = Guid.NewGuid();

        var payload = new
        {
            Subject = "test.subject",
            Payload = "not-valid-json",
            PayloadFormat = "Json",
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PublishMessage_WithInvalidHexFormat_Returns422()
    {
        var envId = Guid.NewGuid();

        var payload = new
        {
            Subject = "test.subject",
            Payload = "ZZZZ",
            PayloadFormat = "HexBytes",
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task StreamEndpoint_WithEmptySubject_Returns400()
    {
        var envId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/stream");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StreamEndpoint_WithSubjectContainingSpaces_Returns400()
    {
        var envId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/stream?subject=orders+with+spaces");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
