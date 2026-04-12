using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NatsManager.Application.Modules.Environments.Commands;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class NatsHealthChecker(
    ICredentialEncryptionService encryptionService,
    ILogger<NatsHealthChecker> logger) : INatsHealthChecker
{
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

    public async Task<TestConnectionResult> CheckHealthAsync(Environment environment, CancellationToken cancellationToken = default)
    {
        string? decryptedCredential = null;
        if (environment.CredentialType != CredentialType.None
            && !string.IsNullOrEmpty(environment.CredentialReference))
        {
            decryptedCredential = encryptionService.Decrypt(environment.CredentialReference);
        }

        return await CheckHealthCoreAsync(environment.ServerUrl, environment.CredentialType, decryptedCredential, cancellationToken);
    }

    public async Task<TestConnectionResult> CheckHealthAsync(string serverUrl, string? credentialReference, CancellationToken cancellationToken = default)
    {
        return await CheckHealthCoreAsync(serverUrl, CredentialType.None, null, cancellationToken);
    }
    private async Task<TestConnectionResult> CheckHealthCoreAsync(string serverUrl, CredentialType credentialType, string? credential, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var opts = new NatsOpts
            {
                Url = serverUrl,
                ConnectTimeout = ConnectionTimeout,
                Name = "NatsManager-HealthCheck",
                AuthOpts = NatsAuthHelper.BuildAuthOpts(credentialType, credential)
            };

            await using var connection = new NatsConnection(opts);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ConnectionTimeout);
            await connection.ConnectAsync();

            sw.Stop();

            var serverVersion = connection.ServerInfo?.Version;
            var jetStreamAvailable = false;

            try
            {
                var js = new NatsJSContext(connection);
                await foreach (var _ in js.ListStreamsAsync(cancellationToken: cts.Token))
                {
                    // Just need to verify JetStream is available by trying to list
                    break;
                }
                jetStreamAvailable = true;
            }
            catch
            {
                // JetStream not available, that's fine
            }

            LogHealthCheckSuccess(serverUrl, (int)sw.ElapsedMilliseconds);

            return new TestConnectionResult(
                Reachable: true,
                LatencyMs: (int)sw.ElapsedMilliseconds,
                ServerVersion: serverVersion,
                JetStreamAvailable: jetStreamAvailable);
        }
        catch (Exception ex)
        {
            sw.Stop();
            LogHealthCheckFailed(serverUrl, ex.Message);

            return new TestConnectionResult(
                Reachable: false,
                LatencyMs: null,
                ServerVersion: null,
                JetStreamAvailable: false);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Health check passed for {Url} in {LatencyMs}ms")]
    private partial void LogHealthCheckSuccess(string url, int latencyMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Health check failed for {Url}: {Error}")]
    private partial void LogHealthCheckFailed(string url, string error);
}
