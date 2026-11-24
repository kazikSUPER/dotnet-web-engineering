using Bakery.Reviews.Application.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bakery.Reviews.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable || string.IsNullOrEmpty(cacheable.CacheKey))
        {
            return await next();
        }

        logger.LogDebug("Кеш {CacheKey}: перевірка L1 → L2 → джерела", cacheable.CacheKey);

        var halfTtl = TimeSpan.FromTicks(cacheable.Ttl.Ticks / 2);
        var l1Ttl = halfTtl < TimeSpan.FromSeconds(30) ? halfTtl : TimeSpan.FromSeconds(30);

        return await cache.GetOrCreateAsync(
            cacheable.CacheKey,
            _ => next(),
            l1Ttl,
            cacheable.Ttl,
            ct);
    }
}
