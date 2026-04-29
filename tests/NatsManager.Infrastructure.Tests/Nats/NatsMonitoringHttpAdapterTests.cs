using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Infrastructure.Nats;
using Shouldly;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class NatsMonitoringHttpAdapterTests
{
    [Fact]
    public async Task FetchSnapshotAsync_WithHealthyResponses_ShouldReturnOkSnapshotAndUseElapsedRate()
    {
        var environment = CreateEnvironment();
        var previous = new MonitoringSnapshot(
            environment.Id,
            DateTimeOffset.UtcNow.AddSeconds(-10),
            new ServerMetrics("2.10.0", 1, 1, 100, 100, 50, 1000, 500, 0, 0, 0, 0, 60, 1024),
            null,
            MonitoringStatus.Ok,
            MonitoringStatus.Ok);
        var adapter = CreateAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"version":"2.10.0","connections":2,"total_connections":3,"max_connections":100,"in_msgs":200,"out_msgs":150,"in_bytes":3000,"out_bytes":2500,"uptime":"1h2m3s","mem":2048}
                """)
        }, request =>
        {
            if (request.RequestUri!.AbsolutePath == "/jsz")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"streams":2,"consumers":4,"messages":1000,"bytes":2048}""")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"ok"}""")
            };
        });

        var snapshot = await adapter.FetchSnapshotAsync(environment, previous, CancellationToken.None);

        snapshot.Status.ShouldBe(MonitoringStatus.Ok);
        snapshot.HealthStatus.ShouldBe(MonitoringStatus.Ok);
        snapshot.Server.Connections.ShouldBe(2);
        snapshot.Server.InMsgsPerSec.ShouldBeGreaterThan(0);
        snapshot.JetStream!.TotalMessages.ShouldBe(1000);
    }

    [Fact]
    public async Task FetchSnapshotAsync_WhenVarzIsUnreachable_ShouldReturnUnavailableSnapshot()
    {
        var environment = CreateEnvironment();
        var adapter = CreateAdapter(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var snapshot = await adapter.FetchSnapshotAsync(environment, null, CancellationToken.None);

        snapshot.Status.ShouldBe(MonitoringStatus.Unavailable);
        snapshot.HealthStatus.ShouldBe(MonitoringStatus.Unavailable);
    }

    [Fact]
    public async Task FetchSnapshotAsync_WhenVarzIsMalformed_ShouldReturnDegradedSnapshot()
    {
        var environment = CreateEnvironment();
        var adapter = CreateAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{")
        });

        var snapshot = await adapter.FetchSnapshotAsync(environment, null, CancellationToken.None);

        snapshot.Status.ShouldBe(MonitoringStatus.Degraded);
        snapshot.HealthStatus.ShouldBe(MonitoringStatus.Unavailable);
    }

    [Fact]
    public async Task FetchSnapshotAsync_WhenPartialResponseIsMalformed_ShouldReturnDegradedSnapshotWithServerMetrics()
    {
        var environment = CreateEnvironment();
        var adapter = CreateAdapter(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/varz")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"version":"2.10.0","connections":2,"total_connections":3,"max_connections":100,"in_msgs":200,"out_msgs":150,"in_bytes":3000,"out_bytes":2500,"uptime":"1s","mem":2048}
                        """)
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{")
            };
        });

        var snapshot = await adapter.FetchSnapshotAsync(environment, null, CancellationToken.None);

        snapshot.Status.ShouldBe(MonitoringStatus.Degraded);
        snapshot.Server.Version.ShouldBe("2.10.0");
    }

    [Fact]
    public async Task FetchSnapshotAsync_WhenJetStreamEndpointIsMissing_ShouldReturnOkSnapshotWithoutJetStreamMetrics()
    {
        var environment = CreateEnvironment();
        var adapter = CreateAdapter(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/varz")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        {"version":"2.10.0","connections":2,"total_connections":3,"max_connections":100,"in_msgs":200,"out_msgs":150,"in_bytes":3000,"out_bytes":2500,"uptime":"1s","mem":2048}
                        """)
                };
            }

            if (request.RequestUri!.AbsolutePath == "/jsz")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"ok"}""")
            };
        });

        var snapshot = await adapter.FetchSnapshotAsync(environment, null, CancellationToken.None);

        snapshot.Status.ShouldBe(MonitoringStatus.Ok);
        snapshot.HealthStatus.ShouldBe(MonitoringStatus.Ok);
        snapshot.JetStream.ShouldBeNull();
        snapshot.Server.Version.ShouldBe("2.10.0");
    }

    private static Environment CreateEnvironment()
    {
        var environment = Environment.Create("test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        return environment;
    }

    private static NatsMonitoringHttpAdapter CreateAdapter(params Func<HttpRequestMessage, HttpResponseMessage>[] handlers)
    {
        var handler = new DelegateHandler(request =>
        {
            var index = request.RequestUri!.AbsolutePath == "/varz" ? 0 : 1;
            var selected = handlers.Length > index ? handlers[index] : handlers[0];
            return selected(request);
        });
        return new NatsMonitoringHttpAdapter(
            new FakeHttpClientFactory(new HttpClient(handler)),
            NullLogger<NatsMonitoringHttpAdapter>.Instance);
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
