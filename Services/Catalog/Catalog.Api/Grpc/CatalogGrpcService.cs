using System.Globalization;
using Catalog.Api.Grpc;
using Catalog.BLL.Interfaces;
using Grpc.Core;

namespace Catalog.Api.Grpc;

public sealed class CatalogGrpcService(IProductService products) : CatalogService.CatalogServiceBase
{
    public override async Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be positive."));
        }

        try
        {
            var product = await products.GetByIdAsync(request.Id, context.CancellationToken);
            if (product is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} was not found."));
            }

            return new ProductReply
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                Stock = product.Stock,
                IsAvailable = product.IsAvailable,
            };
        }
        catch (Catalog.Domain.Exceptions.NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (Catalog.Domain.Exceptions.BusinessConflictException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (Catalog.Domain.Exceptions.DomainValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<ProductsByCategoryReply> GetProductsByCategory(GetProductsByCategoryRequest request, ServerCallContext context)
    {
        if (request.CategoryId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Category id must be positive."));
        }

        var items = await products.GetByCategoryAsync(request.CategoryId, ct: context.CancellationToken);

        var reply = new ProductsByCategoryReply();
        foreach (var item in items)
        {
            reply.Items.Add(new ProductInCategoryReply
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price.ToString("0.00", CultureInfo.InvariantCulture),
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName,
                AddedAt = item.AddedAt.ToString("O"),
            });
        }

        return reply;
    }
}
