using Bakery.Reviews.Application.Caching;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bakery.Reviews.Application.Behaviors;

public sealed class CachingBehavior<TRequest, TResponse>(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        if (cache.TryGetValue(cacheable.CacheKey, out var stored) && stored is TResponse cached)
        {
            logger.LogDebug("Кеш {CacheKey}: потрапляння", cacheable.CacheKey);

            return cached;
        }

        logger.LogDebug("Кеш {CacheKey}: промах, виконуємо запит до бази", cacheable.CacheKey);

        var response = await next();

        cache.Set(
            cacheable.CacheKey,
            response,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheable.Ttl,
                Size = 1,
            });

        return response;
    }
}
