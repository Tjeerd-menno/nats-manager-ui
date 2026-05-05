using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Domain.Modules.Auth;
using NatsEnvironment = NatsManager.Domain.Modules.Environments.Environment;

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
    public async Task GetClients_ShouldReturn200WithClientList()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.ListClientsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<NatsClientInfo>
            {
                new(1, "client-one", "ACC", "127.0.0.1", 4222, 10, 20, 100, 200, TimeSpan.FromSeconds(30)),
            });

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/clients");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<NatsClientInfo>>();
        body.ShouldNotBeNull();
        body!.Single().Name.ShouldBe("client-one");
    }

    [Fact]
    public async Task PublishMessage_InProductionAsOperator_ShouldReturn403()
    {
        var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Operator);
        var env = NatsEnvironment.Create("prod", "nats://localhost:4222", isProduction: true);
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);

        var payload = new { Subject = "test.subject", Payload = "hello" };

        var response = await client.PostAsJsonAsync($"/api/environments/{env.Id}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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
    public async Task PublishMessage_WithInvalidJsonFormat_Returns400()
    {
        var envId = Guid.NewGuid();

        var payload = new
        {
            Subject = "test.subject",
            Payload = "not-valid-json",
            PayloadFormat = "Json",
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishMessage_WithInvalidHexFormat_Returns400()
    {
        var envId = Guid.NewGuid();

        var payload = new
        {
            Subject = "test.subject",
            Payload = "ZZZZ",
            PayloadFormat = "HexBytes",
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishMessage_WithWhitespaceHeaderKey_Returns400()
    {
        var envId = Guid.NewGuid();

        var payload = new
        {
            Subject = "test.subject",
            Payload = "hello",
            Headers = new Dictionary<string, string> { ["   "] = "value" },
        };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StreamEndpoint_WithValidSubject_ReturnsEventStreamContentType()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.SubscribeAsync(envId, "test.>", Arg.Any<CancellationToken>())
            .Returns(EmptyMessages());

        var response = await _client.GetAsync(
            $"/api/environments/{envId}/core-nats/stream?subject=test.%3E",
            HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");
    }

    [Fact]
    public async Task StreamEndpoint_WhenNoMessagesAvailable_FlushesInitialEventStreamFrame()
    {
        var envId = Guid.NewGuid();
        using var requestCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        _factory.CoreNatsAdapter.SubscribeAsync(envId, "test.>", Arg.Any<CancellationToken>())
            .Returns(call => NeverMessages(call.ArgAt<CancellationToken>(2)));

        using var response = await _client.GetAsync(
            $"/api/environments/{envId}/core-nats/stream?subject=test.%3E",
            HttpCompletionOption.ResponseHeadersRead,
            requestCts.Token);
        await using var stream = await response.Content.ReadAsStreamAsync(requestCts.Token);

        var initialFrame = await ReadUntilDelimiterAsync(stream, "\n\n", requestCts.Token)
            .WaitAsync(TimeSpan.FromSeconds(1), requestCts.Token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        initialFrame.ShouldBe(": subscribed\n\n");
    }

    private static async Task<string> ReadUntilDelimiterAsync(
        Stream stream,
        string delimiter,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64];
        using var collected = new MemoryStream();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            bytesRead.ShouldBeGreaterThan(0);

            await collected.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

            var text = Encoding.UTF8.GetString(collected.ToArray());
            if (text.EndsWith(delimiter, StringComparison.Ordinal))
            {
                return text;
            }
        }
    }

    private static async IAsyncEnumerable<NatsLiveMessage> EmptyMessages(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken _ = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<NatsLiveMessage> NeverMessages(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        yield break;
    }

    [Fact]
    public async Task StreamEndpoint_WithEmptySubject_Returns400()
    {
        var envId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/stream");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errors").GetProperty("subject").EnumerateArray().Single().GetString()
            .ShouldBe("Subject pattern must not be empty.");
    }

    [Fact]
    public async Task StreamEndpoint_WithSubjectContainingSpaces_Returns400()
    {
        var envId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/stream?subject=orders+with+spaces");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errors").GetProperty("subject").EnumerateArray().Single().GetString()
            .ShouldBe("Subject pattern must not contain spaces.");
    }
}
