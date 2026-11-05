using Bakery.Reviews.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bakery.Reviews.Application.Behaviors;

public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            return await next();
        }
        catch (ValidationException exception)
        {
            logger.LogWarning(exception, "Валідація відхилила запит {RequestName}", requestName);
            throw;
        }
        catch (DomainException exception)
        {
            logger.LogWarning(exception, "Доменне порушення у запиті {RequestName}: {Message}", requestName, exception.Message);
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Неочікувана помилка під час виконання {RequestName}", requestName);
            throw;
        }
    }
}
