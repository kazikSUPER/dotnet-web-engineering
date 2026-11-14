using AutoMapper;
using Catalog.BLL.Caching;
using Catalog.BLL.Diagnostics;
using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Catalog.DAL.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Catalog.BLL.Services;

public class ProductService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<ProductService> logger) : IProductService
{
    public async Task<ProductCardDto?> GetCardAsync(int productId, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.ProductCard(productId),
        async token =>
        {
            using var activity = CatalogActivitySources.Products.StartActivity("catalog.products.card", ActivityKind.Internal);
            activity?.SetTag("catalog.product_id", productId);

            var product = await uow.Products.GetCardAsync(productId, token);

            if (product is null)
            {
                logger.LogInformation("Каталог: товар {ProductId} не знайдено", productId);

                return null;
            }

            var card = mapper.Map<ProductCardDto>(product);

            activity?.SetTag("catalog.stock", card.Stock);
            activity?.SetTag("catalog.category_count", card.Categories.Length);

            logger.LogInformation(
                "Каталог: товар {ProductId} {ProductName}, ціна {Price}, залишок {Stock}, доступність {IsAvailable}, категорій {CategoryCount}",
                card.ProductId,
                card.Name,
                card.Price,
                card.Stock,
                card.IsAvailable,
                card.Categories.Length);

            return card;
        },
        l1Ttl: TimeSpan.FromMinutes(2),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

    public async Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.ProductsByCategory(categoryId, notEarlierThan),
        async token =>
        {
            using var activity = CatalogActivitySources.Products.StartActivity("catalog.products.by-category", ActivityKind.Internal);
            activity?.SetTag("catalog.category_id", categoryId);

            var links = await uow.Products.GetCategoryLinksAsync(categoryId, notEarlierThan, token);
            var products = mapper.Map<IReadOnlyList<ProductInCategoryDto>>(links);

            logger.LogInformation(
                "Каталог: у категорії {CategoryId} знайдено {ProductCount} товарів, фільтровано від {NotEarlierThan}",
                categoryId,
                products.Count,
                notEarlierThan?.ToString("u") ?? "початку");

            return products;
        },
        l1Ttl: TimeSpan.FromMinutes(2),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default) => await cache.GetOrCreateAsync(
        CacheKeys.Product(id),
        async token =>
        {
            using var activity = CatalogActivitySources.Products.StartActivity("catalog.products.by-id", ActivityKind.Internal);
            activity?.SetTag("catalog.product_id", id);

            var product = await uow.Products.GetByIdAsync(id, token);

            if (product is null)
            {
                logger.LogInformation("Каталог: товар {ProductId} не знайдено", id);

                return null;
            }

            var dto = mapper.Map<ProductDto>(product);
            activity?.SetTag("catalog.stock", dto.Stock);

            logger.LogInformation(
                "Каталог: товар {ProductId} {ProductName}, ціна {Price}, залишок {Stock}",
                dto.Id,
                dto.Name,
                dto.Price,
                dto.Stock);

            return dto;
        },
        l1Ttl: TimeSpan.FromMinutes(2),
        l2Ttl: TimeSpan.FromMinutes(5),
        ct);
}