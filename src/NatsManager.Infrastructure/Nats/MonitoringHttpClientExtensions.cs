using System.Net.Http.Json;
using System.Text.Json;

namespace NatsManager.Infrastructure.Nats;

internal enum MonitoringFailureKind
{
    None,
    HttpRequest,
    Timeout,
    Json
}

internal sealed record MonitoringHttpResult<T>(T? Value, MonitoringFailureKind FailureKind, string? ErrorMessage)
    where T : class
{
    public bool IsSuccess => FailureKind == MonitoringFailureKind.None;

    public static MonitoringHttpResult<T> Success(T? value) => new(value, MonitoringFailureKind.None, null);

    public static MonitoringHttpResult<T> Failure(MonitoringFailureKind failureKind, string message) =>
        new(null, failureKind, message);
}

internal static class MonitoringHttpClientExtensions
{
    public static async Task<MonitoringHttpResult<T>> GetJsonWithHandlingAsync<T>(
        this HttpClient client,
        string requestUri,
        CancellationToken cancellationToken,
        Func<HttpResponseMessage, CancellationToken, Task<MonitoringHttpResult<T>>>? responseHandler = null)
        where T : class
    {
        try
        {
            using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (responseHandler is not null)
            {
                return await responseHandler(response, cancellationToken);
            }

            response.EnsureSuccessStatusCode();
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return MonitoringHttpResult<T>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            return MonitoringHttpResult<T>.Failure(MonitoringFailureKind.Timeout, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return MonitoringHttpResult<T>.Failure(MonitoringFailureKind.HttpRequest, ex.Message);
        }
        catch (JsonException ex)
        {
            return MonitoringHttpResult<T>.Failure(MonitoringFailureKind.Json, ex.Message);
        }
    }

    public static async Task<MonitoringHttpResult<T>> ReadJsonOrFailureAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return MonitoringHttpResult<T>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            return MonitoringHttpResult<T>.Failure(MonitoringFailureKind.Timeout, ex.Message);
        }
        catch (JsonException ex)
        {
            return MonitoringHttpResult<T>.Failure(MonitoringFailureKind.Json, ex.Message);
        }
    }
}
