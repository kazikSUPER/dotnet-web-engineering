using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Exceptions;

namespace Bakery.Reviews.Domain.Entities;

public sealed class ProductRating : BaseEntity
{
    private ProductRating()
    {
    }

    public ProductRating(int productId, int totalReviews, double averageScore)
    {
        if (productId <= 0)
        {
            throw new DomainException("Ідентифікатор товару має бути додатним.");
        }

        if (totalReviews < 0)
        {
            throw new DomainException("Кількість відгуків не може бути від'ємною.");
        }

        ProductId = productId;
        TotalReviews = totalReviews;
        AverageScore = Math.Round(averageScore, 2);
        LastUpdated = DateTime.UtcNow;
    }

    public int ProductId { get; private set; }

    public int TotalReviews { get; private set; }

    public double AverageScore { get; private set; }

    public DateTime LastUpdated { get; private set; }

    public void Recalculate(int totalReviews, double averageScore)
    {
        if (totalReviews < 0)
        {
            throw new DomainException("Кількість відгуків не може бути від'ємною.");
        }

        TotalReviews = totalReviews;
        AverageScore = Math.Round(averageScore, 2);
        LastUpdated = DateTime.UtcNow;
        Touch();
    }
}
