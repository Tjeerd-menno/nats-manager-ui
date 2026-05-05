using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Infrastructure.Nats;
using NatsManager.Infrastructure.Nats.ClusterObservability;
using Shouldly;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class NatsClusterMonitoringHttpAdapterTests
{
    [Fact]
    public async Task GetClusterObservationAsync_WhenVarzContainsClusterObject_ShouldReturnHealthyObservation()
    {
        var environment = CreateEnvironment();
        var repository = new StubEnvironmentRepository(environment);

        var adapter = new NatsClusterMonitoringHttpAdapter(
            new FakeHttpClientFactory(new HttpClient(new DelegateHandler(request => request.RequestUri!.AbsolutePath switch
            {
                "/varz" => JsonResponse("""
                    {"server_id":"srv-1","server_name":"n1","cluster":{},"version":"2.12.1","uptime":"7m16s","connections":2,"max_connections":65536,"slow_consumers":0,"in_msgs":39,"out_msgs":33,"in_bytes":398,"out_bytes":9402,"mem":19496960}
                    """),
                "/healthz" => JsonResponse("""{"status":"ok"}"""),
                "/jsz" => JsonResponse("""{"streams":1,"consumers":0,"messages":0,"bytes":0}"""),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound)
            }))),
            repository,
            Options.Create(new MonitoringOptions()),
            NullLogger<NatsClusterMonitoringHttpAdapter>.Instance);

        var observation = await adapter.GetClusterObservationAsync(environment.Id, CancellationToken.None);

        observation.Status.ShouldBe(ClusterStatus.Healthy);
        observation.ServerCount.ShouldBe(1);
        observation.ConnectionCount.ShouldBe(2);
        observation.Servers.Count.ShouldBe(1);
        observation.Servers[0].ClusterName.ShouldBeNull();
    }

    private static Environment CreateEnvironment()
    {
        var environment = Environment.Create("test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        return environment;
    }

    private static HttpResponseMessage JsonResponse(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }

    private sealed class StubEnvironmentRepository(Environment environment) : IEnvironmentRepository
    {
        public Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == environment.Id ? environment : null);

        public Task<Environment?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Environment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Environment> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? search = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(Environment environment, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Environment environment, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(Environment environment, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Environment>> GetEnabledAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
