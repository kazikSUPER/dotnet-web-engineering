using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Platform.Aggregator.HealthChecks;

public sealed class DownstreamGrpcHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DownstreamGrpcHealthCheck> _logger;

    public DownstreamGrpcHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<DownstreamGrpcHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var services = new Dictionary<string, string>
        {
            ["orders-grpc"] = "http://_grpc.orders-api",
            ["catalog-grpc"] = "http://_grpc.catalog-api",
            ["reviews-grpc"] = "http://_grpc.reviews-api"
        };

        var results = new Dictionary<string, object>();
        var anyCriticalFailed = false;
        var anyNonCriticalFailed = false;

        foreach (var (name, address) in services)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2));

                using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
                {
                    HttpClient = _httpClientFactory.CreateClient()
                });

                var client = new Health.HealthClient(channel);
                var response = await client.CheckAsync(
                    new HealthCheckRequest { Service = "" },
                    deadline: DateTime.UtcNow.AddSeconds(2),
                    cancellationToken: cts.Token);

                var isHealthy = response.Status == HealthCheckResponse.Types.ServingStatus.Serving;
                results[name] = response.Status.ToString();

                if (!isHealthy)
                {
                    if (name.Contains("reviews"))
                        anyNonCriticalFailed = true;
                    else
                        anyCriticalFailed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Downstream gRPC health check probe failed for {ServiceName}", name);
                results[name] = $"Unavailable: {ex.Message}";
                if (name.Contains("reviews"))
                    anyNonCriticalFailed = true;
                else
                    anyCriticalFailed = true;
            }
        }

        if (anyCriticalFailed)
        {
            return HealthCheckResult.Unhealthy("One or more critical downstream gRPC services are unhealthy.", data: results);
        }

        if (anyNonCriticalFailed)
        {
            return HealthCheckResult.Degraded("Non-critical downstream gRPC service (Reviews) is unhealthy.", data: results);
        }

        return HealthCheckResult.Healthy("All downstream gRPC services are healthy.", results);
    }
}
