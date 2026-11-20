using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Platform.Aggregator.Diagnostics;

namespace Platform.Aggregator.Clients;

public abstract class DownstreamClient(HttpClient http, ILogger logger, string serviceName)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string ServiceName { get; } = serviceName;

    protected async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        using var activity = AggregatorActivitySources.Assembly.StartActivity($"downstream.{ServiceName}", ActivityKind.Client);
        activity?.SetTag("downstream.service", ServiceName);
        activity?.SetTag("downstream.path", path);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await http.GetAsync(path, ct);
            stopwatch.Stop();

            var elapsed = stopwatch.Elapsed.TotalMilliseconds;

            activity?.SetTag("downstream.status", (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(
                    "Відповідь сервісу {ServiceName} {Path}: {StatusCode} за {ElapsedMilliseconds:0.0} мс",
                    ServiceName, path, (int)response.StatusCode, elapsed);

                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Відповідь сервісу {ServiceName} {Path}: {StatusCode} за {ElapsedMilliseconds:0.0} мс",
                    ServiceName, path, (int)response.StatusCode, elapsed);

                throw new DownstreamServiceException(ServiceName, path, response.StatusCode, $"HTTP {(int)response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, ct);

            logger.LogInformation(
                "Відповідь сервісу {ServiceName} {Path}: {StatusCode} за {ElapsedMilliseconds:0.0} мс",
                ServiceName, path, (int)response.StatusCode, elapsed);

            return payload;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();

            logger.LogWarning(
                "Таймаут виклику {ServiceName} {Path} після {ElapsedMilliseconds:0.0} мс",
                ServiceName, path, stopwatch.Elapsed.TotalMilliseconds);

            throw new DownstreamServiceException(ServiceName, path, null, "таймаут вичерпано");
        }
        catch (HttpRequestException exception)
        {
            stopwatch.Stop();

            logger.LogWarning(
                exception,
                "Сервіс {ServiceName} недоступний на {Path} після {ElapsedMilliseconds:0.0} мс",
                ServiceName, path, stopwatch.Elapsed.TotalMilliseconds);

            throw new DownstreamServiceException(ServiceName, path, null, exception.Message);
        }
    }
}
