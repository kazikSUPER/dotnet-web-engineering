using Bakery.Reviews.Domain.Common;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.ValueObjects;

namespace Bakery.Reviews.Domain.Entities;

public sealed class Review : BaseEntity
{
    public const int MaxTags = 10;
    public const int MaxTagLength = 30;

    private List<string> tags = [];

    private Review()
    {
        CustomerName = null!;
        Comment = null!;
        Ratings = null!;
    }

    public Review(int orderId, int customerId, PersonName customerName, int productId, string productName, ReviewComment comment, Rating ratings, IEnumerable<string>? tagValues = null)
    {
        if (orderId <= 0)
        {
            throw new DomainException("Ідентифікатор замовлення має бути додатним.");
        }

        if (customerId <= 0)
        {
            throw new DomainException("Ідентифікатор клієнта має бути додатним.");
        }

        if (productId <= 0)
        {
            throw new DomainException("Ідентифікатор товару має бути додатним.");
        }

        OrderId = orderId;
        CustomerId = customerId;
        CustomerName = customerName;
        ProductId = productId;
        ProductName = NormalizeProductName(productName);
        Comment = comment;
        Ratings = ratings;
        SyncAverageRating();
        ReplaceTags(tagValues);
        Status = ReviewStatus.Pending;
    }

    public int OrderId { get; private set; }

    public int CustomerId { get; private set; }

    public PersonName CustomerName { get; private set; }

    public int ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public ReviewComment Comment { get; private set; }

    public Rating Ratings { get; private set; }

    public double AverageRating { get; private set; }

    public IReadOnlyCollection<string> Tags => tags;

    public ReviewStatus Status { get; private set; }

    public AdminResponse? AdminResponse { get; private set; }

    public static Review Create(int orderId, int customerId, string customerName, int productId, string productName, string comment, int taste, int freshness, int aroma, IEnumerable<string>? tagValues = null)
    {
        return new Review(orderId, customerId, new PersonName(customerName), productId, productName, new ReviewComment(comment), new Rating(taste, freshness, aroma), tagValues);
    }

    public void UpdateContent(ReviewComment comment, Rating ratings, IEnumerable<string>? tagValues = null)
    {
        if (Status == ReviewStatus.Rejected)
        {
            throw new ConflictException("Відхилений відгук редагувати не можна.");
        }

        Comment = comment;
        Ratings = ratings;
        SyncAverageRating();

        if (tagValues is not null)
        {
            ReplaceTags(tagValues);
        }

        Touch();
    }

    public void Approve()
    {
        Moderate(ReviewStatus.Approved);
    }

    public void Reject()
    {
        Moderate(ReviewStatus.Rejected);
    }

    public void Flag()
    {
        Moderate(ReviewStatus.Flagged);
    }

    public void AddAdminResponse(AdminResponse response)
    {
        if (Status != ReviewStatus.Approved)
        {
            throw new ConflictException($"Відповідь адміністратора можлива лише для схваленого відгуку, поточний статус — {Status}.");
        }

        if (AdminResponse is not null)
        {
            throw new ConflictException("Відповідь адміністратора вже додано до цього відгуку.");
        }

        AdminResponse = response;
        Touch();
    }

    public void RenameProduct(string productName)
    {
        ProductName = NormalizeProductName(productName);
        Touch();
    }

    public void ChangeCustomerName(PersonName name)
    {
        CustomerName = name;
        Touch();
    }

    public bool IsPending => Status == ReviewStatus.Pending;

    private void Moderate(ReviewStatus next)
    {
        if (!IsPending)
        {
            throw new ConflictException($"Відгук уже модерувано: поточний статус — {Status}.");
        }

        Status = next;
        Touch();
    }

    private void SyncAverageRating()
    {
        AverageRating = Ratings.Average;
    }

    private void ReplaceTags(IEnumerable<string>? tagValues)
    {
        tags.Clear();

        if (tagValues is null)
        {
            return;
        }

        var normalized = tagValues
            .Select(x => (x ?? string.Empty).Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .Distinct()
            .ToArray();

        if (normalized.Length > MaxTags)
        {
            throw new DomainException($"Відгук може містити не більше {MaxTags} тегів.");
        }

        if (normalized.Any(x => x.Length > MaxTagLength))
        {
            throw new DomainException($"Тег не може перевищувати {MaxTagLength} символів.");
        }

        tags.AddRange(normalized);
    }

    private static string NormalizeProductName(string productName)
    {
        var trimmed = (productName ?? string.Empty).Trim();

        if (trimmed.Length is < 1 or > 100)
        {
            throw new DomainException("Назва товару має бути від 1 до 100 символів.");
        }

        return trimmed;
    }
}
