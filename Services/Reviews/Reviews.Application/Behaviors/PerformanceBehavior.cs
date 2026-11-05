using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bakery.Reviews.Application.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowThresholdMilliseconds = 500;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next();
        }
        finally
        {
            stopwatch.Stop();

            var requestName = typeof(TRequest).Name;

            if (stopwatch.ElapsedMilliseconds > SlowThresholdMilliseconds)
            {
                logger.LogWarning(
                    "Повільний обробник {RequestName}: {ElapsedMilliseconds} мс (поріг {ThresholdMilliseconds} мс)",
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    SlowThresholdMilliseconds);
            }
            else
            {
                logger.LogDebug("Обробник {RequestName} виконано за {ElapsedMilliseconds} мс", requestName, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
