using MediatR;
using Microsoft.Extensions.Logging;

namespace Bakery.Reviews.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Запит {RequestName} прийнято, відповідь {ResponseName}", requestName, typeof(TResponse).Name);
        logger.LogDebug("Вхідні дані {RequestName}: {@Request}", requestName, request);

        var response = await next();

        logger.LogInformation("Запит {RequestName} оброблено", requestName);

        return response;
    }
}
