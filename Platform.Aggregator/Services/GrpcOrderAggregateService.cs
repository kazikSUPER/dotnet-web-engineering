using System.Collections.Concurrent;
using System.Globalization;
using Catalog.Api.Grpc;
using Grpc.Core;
using Orders.Api.Grpc;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Models;
using Reviews.WebApi.Grpc;

namespace Platform.Aggregator.Services;

public sealed class GrpcOrderAggregateService(
    OrdersService.OrdersServiceClient ordersClient,
    CatalogService.CatalogServiceClient catalogClient,
    ReviewsService.ReviewsServiceClient reviewsClient,
    IAggregatedCache cache,
    ILogger<GrpcOrderAggregateService> logger)
{
    private static readonly StatusCode[] Degradable = [StatusCode.Unavailable, StatusCode.DeadlineExceeded];

    public async Task<GrpcOrderAggregateDto?> GetOrderFullAsync(int orderId, CancellationToken ct)
    {
        var key = AggregatedCache.OrderFull(orderId);
        var cached = await cache.TryGetAsync<GrpcOrderAggregateDto>(key, ct);
        if (cached is not null)
        {
            return cached with { Cached = true };
        }

        OrderReply order;
        try
        {
            order = await ordersClient.GetOrderAsync(new GetOrderRequest { Id = orderId }, cancellationToken: ct);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException exception) when (Degradable.Contains(exception.StatusCode))
        {
            throw new GrpcDownstreamUnavailableException("orders", exception);
        }

        var warnings = new ConcurrentQueue<string>();
        var catalogTasks = order.Items.Select(item => GetCatalogAsync(item.ProductId, warnings, ct)).ToArray();
        var ratingTasks = order.Items.Select(item => GetRatingAsync(item.ProductId, warnings, ct)).ToArray();
        await Task.WhenAll(catalogTasks.Cast<Task>().Concat(ratingTasks));

        var catalog = await Task.WhenAll(catalogTasks);
        var ratings = await Task.WhenAll(ratingTasks);
        var products = order.Items.Select((item, index) => new GrpcOrderProductDto(
            item.ProductId,
            item.ProductName,
            ParseDecimal(item.UnitPrice),
            item.Quantity,
            catalog[index],
            ratings[index])).ToList();

        var result = new GrpcOrderAggregateDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.CustomerName,
            order.Status,
            ParseDecimal(order.TotalAmount),
            products,
            false,
            warnings.ToList());

        if (warnings.IsEmpty)
        {
            await cache.SetAsync(key, result, AggregatedCache.Ttl, ct);
        }
        else
        {
            logger.LogWarning("Частковий gRPC-агрегат замовлення {OrderId} не кешується: {WarningCount} попереджень", orderId, warnings.Count);
        }

        return result;
    }

    private async Task<GrpcCatalogProductSnapshot?> GetCatalogAsync(int productId, ConcurrentQueue<string> warnings, CancellationToken ct)
    {
        try
        {
            var result = await catalogClient.GetProductAsync(new GetProductRequest { Id = productId }, cancellationToken: ct);
            return new GrpcCatalogProductSnapshot(result.Name, ParseDecimal(result.Price), result.Stock, result.IsAvailable);
        }
        catch (RpcException exception) when (Degradable.Contains(exception.StatusCode))
        {
            warnings.Enqueue($"catalog:product:{productId} unavailable ({exception.StatusCode})");
            logger.LogWarning(exception, "Catalog gRPC недоступний для товару {ProductId}", productId);
            return null;
        }
    }

    private async Task<GrpcProductRatingSnapshot?> GetRatingAsync(int productId, ConcurrentQueue<string> warnings, CancellationToken ct)
    {
        try
        {
            var result = await reviewsClient.GetProductRatingAsync(new GetProductRatingRequest { ProductId = productId }, cancellationToken: ct);
            return new GrpcProductRatingSnapshot(result.TotalReviews, result.AverageScore);
        }
        catch (RpcException exception) when (Degradable.Contains(exception.StatusCode))
        {
            warnings.Enqueue($"reviews:product:{productId} unavailable ({exception.StatusCode})");
            logger.LogWarning(exception, "Reviews gRPC недоступний для товару {ProductId}", productId);
            return null;
        }
    }

    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : 0m;
}

public sealed class GrpcDownstreamUnavailableException(string serviceName, Exception innerException)
    : Exception($"gRPC service {serviceName} is unavailable.", innerException)
{
    public string ServiceName { get; } = serviceName;
}
