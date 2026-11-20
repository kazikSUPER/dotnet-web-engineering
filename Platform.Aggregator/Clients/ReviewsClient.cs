using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class ReviewsClient(HttpClient http, ILogger<ReviewsClient> logger)
    : DownstreamClient(http, logger, "reviews-api")
{
    public Task<ProductRatingContract?> GetRatingAsync(int productId, CancellationToken ct) =>
        GetAsync<ProductRatingContract>($"api/product-ratings/{productId}", ct);

    public Task<ReviewPageContract?> GetApprovedReviewsAsync(int productId, int limit, CancellationToken ct) =>
        GetAsync<ReviewPageContract>($"api/reviews?productId={productId}&status=Approved&sort=CreatedAt&descending=true&limit={limit}", ct);

    public Task<ReviewPageContract?> GetTopRatedReviewsAsync(int productId, int limit, CancellationToken ct) =>
        GetAsync<ReviewPageContract>($"api/reviews?productId={productId}&status=Approved&minAverage=4.5&sort=AverageRating&descending=true&limit={limit}", ct);

    public Task<ReviewPageContract?> GetCustomerReviewsAsync(int customerId, int limit, CancellationToken ct) =>
        GetAsync<ReviewPageContract>($"api/reviews?customerId={customerId}&sort=CreatedAt&descending=true&limit={limit}", ct);
}
