using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Contracts;
using Platform.Aggregator.Diagnostics;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public sealed class OrderAssemblyService(
    OrdersClient orders,
    CatalogClient catalog,
    ReviewsClient reviews,
    ILogger<OrderAssemblyService> logger)
{
    private const int ReviewsPerProduct = 3;

    private const int CustomerReviewsLimit = 5;

    public async Task<OrderAggregateDto?> AssembleAsync(int orderId, CancellationToken ct)
    {
        using var activity = AggregatorActivitySources.Assembly.StartActivity("aggregator.order.assemble", ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();

        var order = await orders.GetOrderAsync(orderId, ct);

        if (order is null)
        {
            logger.LogInformation("Замовлення {OrderId} не знайдено у сервісі замовлень", orderId);

            return null;
        }

        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.number", order.OrderNumber);
        activity?.SetTag("order.status", order.Status);
        activity?.SetTag("order.total_amount", order.TotalAmount);
        activity?.SetTag("order.item_count", order.Items.Length);

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToArray();
        var failures = new PartialFailureCollector(logger);

        var customerTask = order.CustomerId > 0
            ? failures.CallAsync($"клієнт {order.CustomerId} сервісу замовлень", () => orders.GetCustomerAsync(order.CustomerId, ct))
            : Task.FromResult<CustomerContract?>(null);

        var paymentTask = failures.CallAsync($"оплати замовлення {orderId}", () => orders.GetPaymentAsync(orderId, ct));
        var historyTask = failures.CallAsync($"історії статусів замовлення {orderId}", () => orders.GetStatusHistoryAsync(orderId, ct));

        var cardTasks = productIds
            .Select(id => failures.CallAsync($"картки товару {id} сервісу каталогу", () => catalog.GetProductAsync(id, ct)))
            .ToArray();

        var ratingTasks = productIds
            .Select(id => failures.CallAsync($"рейтингу товару {id} сервісу відгуків", () => reviews.GetRatingAsync(id, ct)))
            .ToArray();

        var reviewPageTasks = productIds
            .Select(id => failures.CallAsync($"схвалених відгуків товару {id}", () => reviews.GetApprovedReviewsAsync(id, ReviewsPerProduct, ct)))
            .ToArray();

        var customerReviewsTask = order.CustomerId > 0
            ? failures.CallAsync($"відгуків клієнта {order.CustomerId}", () => reviews.GetCustomerReviewsAsync(order.CustomerId, CustomerReviewsLimit, ct))
            : Task.FromResult<ReviewPageContract?>(null);

        var tasks = cardTasks.Cast<Task>()
            .Concat(ratingTasks)
            .Concat(reviewPageTasks)
            .Concat([customerTask, paymentTask, historyTask, customerReviewsTask]);

        await Task.WhenAll(tasks);

        var customerReviews = await customerReviewsTask;

        var cards = await Task.WhenAll(cardTasks);
        var ratings = await Task.WhenAll(ratingTasks);
        var reviewPages = await Task.WhenAll(reviewPageTasks);

        var lines = order.Items
            .Select(item => new OrderLineDto(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.TotalPrice,
                cards[Array.IndexOf(productIds, item.ProductId)]))
            .ToArray();

        var feedback = productIds
            .Select((id, index) => new ProductFeedbackDto(id, ratings[index], reviewPages[index]?.Items ?? []))
            .ToArray();

        stopwatch.Stop();

        var aggregate = new OrderAggregateDto(
            order,
            lines,
            feedback,
            failures.Snapshot(),
            1 + productIds.Length * 3 + 4,
            stopwatch.Elapsed.TotalMilliseconds,
            DateTimeOffset.UtcNow)
        {
            Customer = await customerTask,
            Payment = await paymentTask,
            StatusHistory = await historyTask ?? [],
            CustomerReviews = customerReviews?.Items ?? [],
        };

        activity?.SetTag("aggregator.warning_count", aggregate.Warnings.Count);

        logger.LogInformation(
            "Замовлення {OrderId} агреговано: {LineCount} позицій, {ProductCount} товарів, {MissingCatalogCount} без картки каталогу, {MissingFeedbackCount} без відгуків, {CustomerReviewCount} відгуків клієнта, {WarningCount} попереджень за {ElapsedMilliseconds:0.0} мс",
            order.Id,
            lines.Length,
            productIds.Length,
            cards.Count(card => card is null),
            reviewPages.Count(page => page is null),
            aggregate.CustomerReviews.Count,
            aggregate.Warnings.Count,
            aggregate.AssemblyDurationMilliseconds);

        return aggregate;
    }
}
