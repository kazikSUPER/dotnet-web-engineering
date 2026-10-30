using Serilog.Context;

namespace Bakery.API.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string UserIdHeader = "X-User-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId)) correlationId = context.TraceIdentifier;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        var properties = new List<IDisposable> { LogContext.PushProperty("CorrelationId", correlationId) };
        var userId = context.Request.Headers[UserIdHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(userId)) properties.Add(LogContext.PushProperty("UserId", userId));

        try
        {
            await next(context);
        }
        finally
        {
            for (var i = properties.Count - 1; i >= 0; i--) properties[i].Dispose();
        }
    }
}
