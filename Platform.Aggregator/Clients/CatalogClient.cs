using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
    : DownstreamClient(http, logger, "catalog-api")
{
    public Task<ProductCardContract?> GetProductAsync(int productId, CancellationToken ct) =>
        GetAsync<ProductCardContract>($"api/products/{productId}", ct);
}
