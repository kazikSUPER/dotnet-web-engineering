using System.Diagnostics;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Model;

namespace Platform.ApiGateway;

public sealed class RouteLoggingMiddleware(RequestDelegate next, ILogger<RouteLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            Write(context, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private void Write(HttpContext context, double elapsedMilliseconds)
    {
        var feature = context.Features.Get<IReverseProxyFeature>();
        var statusCode = context.Response.StatusCode;
        var routeId = feature?.Route?.Config?.RouteId;
        var clusterId = feature?.Cluster?.Config?.ClusterId;

        var fields = new object[]
        {
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            routeId ?? "unmatched",
            clusterId ?? "none",
            statusCode,
            elapsedMilliseconds,
            CorrelationIdMiddleware.Of(context),
        };

        if (routeId is null)
        {
            logger.LogWarning(
                "Маршрутизація не відбулась: {Method} {Path} маршрут {RouteId} кластер {ClusterId} статус {StatusCode} тривалість {ElapsedMilliseconds:0.0} мс кореляція {CorrelationId}",
                fields);

            return;
        }

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                "Проксі-помилка: {Method} {Path} маршрут {RouteId} кластер {ClusterId} статус {StatusCode} тривалість {ElapsedMilliseconds:0.0} мс кореляція {CorrelationId}",
                fields);

            return;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            logger.LogWarning(
                "Проксі-відмова: {Method} {Path} маршрут {RouteId} кластер {ClusterId} статус {StatusCode} тривалість {ElapsedMilliseconds:0.0} мс кореляція {CorrelationId}",
                fields);

            return;
        }

        logger.LogInformation(
            "Проксі: {Method} {Path} маршрут {RouteId} кластер {ClusterId} статус {StatusCode} тривалість {ElapsedMilliseconds:0.0} мс кореляція {CorrelationId}",
            fields);
    }
}
