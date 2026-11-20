using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Diagnostics;
using Platform.Aggregator.Models;

namespace Platform.Aggregator.Services;

public sealed class ProductOverviewService(
    CatalogClient catalog,
    ReviewsClient reviews,
    ILogger<ProductOverviewService> logger)
{
    private const int ReviewPreviewLimit = 5;

    public async Task<ProductOverviewDto?> BuildAsync(int productId, CancellationToken ct)
    {
        using var activity = AggregatorActivitySources.Assembly.StartActivity("aggregator.product.overview", ActivityKind.Internal);
        var stopwatch = Stopwatch.StartNew();
        var failures = new PartialFailureCollector(logger);

        activity?.SetTag("product.id", productId);

        var product = await catalog.GetProductAsync(productId, ct);

        if (product is null)
        {
            logger.LogInformation("Товар {ProductId} не знайдено в сервісі каталогу", productId);

            return null;
        }

        var ratingTask = failures.CallAsync($"рейтингу товару {productId}", () => reviews.GetRatingAsync(productId, ct));
        var reviewsTask = failures.CallAsync($"схвалених відгуків товару {productId}", () => reviews.GetApprovedReviewsAsync(productId, ReviewPreviewLimit, ct));
        var topRatedTask = failures.CallAsync($"найкращих відгуків товару {productId}", () => reviews.GetTopRatedReviewsAsync(productId, ReviewPreviewLimit, ct));

        await Task.WhenAll(ratingTask, reviewsTask, topRatedTask);

        var rating = await ratingTask;
        var approved = await reviewsTask;
        var topRated = await topRatedTask;

        stopwatch.Stop();

        var highlights = (topRated?.Items ?? [])
            .Concat(approved?.Items ?? [])
            .DistinctBy(review => review.Id)
            .ToArray();

        var overview = new ProductOverviewDto(
            productId,
            product,
            rating,
            highlights,
            failures.Snapshot(),
            3,
            stopwatch.Elapsed.TotalMilliseconds,
            DateTimeOffset.UtcNow);

        activity?.SetTag("aggregator.warning_count", overview.Warnings.Count);

        logger.LogInformation(
            "Товар {ProductId} ({ProductName}) агреговано: {AverageScore:0.0} із {TotalReviews} оцінок, {ReviewCount} відгуків, {WarningCount} попереджень за {ElapsedMilliseconds:0.0} мс",
            productId,
            product.Name,
            rating?.AverageScore ?? 0,
            rating?.TotalReviews ?? 0,
            overview.Reviews.Count,
            overview.Warnings.Count,
            overview.AssemblyDurationMilliseconds);

        return overview;
    }
}
