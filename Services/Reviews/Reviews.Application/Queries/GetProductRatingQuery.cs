using AutoMapper;
using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;

namespace Bakery.Reviews.Application.Queries;

public sealed record GetProductRatingQuery(int ProductId) : IQuery<ProductRatingDto>, ICacheableQuery
{
    public string CacheKey => $"reviews:product-rating:{ProductId}";

    public TimeSpan Ttl => TimeSpan.FromSeconds(60);
}

public sealed class GetProductRatingQueryHandler(IProductRatingRepository repository, IMapper mapper)
    : IRequestHandler<GetProductRatingQuery, ProductRatingDto>
{
    public async Task<ProductRatingDto> Handle(GetProductRatingQuery request, CancellationToken ct)
    {
        var rating = await repository.GetByProductIdAsync(request.ProductId, ct)
            ?? throw new NotFoundException("Рейтинг товару", request.ProductId.ToString());

        return mapper.Map<ProductRatingDto>(rating);
    }
}
