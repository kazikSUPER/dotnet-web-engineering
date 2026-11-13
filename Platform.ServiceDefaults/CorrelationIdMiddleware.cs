using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string UserIdHeaderName = "X-User-Id";
    public const string ItemName = "CorrelationId";

    private const int MaxAcceptedLength = 64;

    public static string Of(HttpContext context) =>
        context.Items[ItemName] as string ?? Activity.Current?.TraceId.ToString() ?? "unavailable";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Normalize(context.Request.Headers[HeaderName].FirstOrDefault());
        var wasSuppliedByClient = string.IsNullOrWhiteSpace(correlationId) is false;

        correlationId ??= Guid.NewGuid().ToString("N");

        var userId = Normalize(context.Request.Headers[UserIdHeaderName].FirstOrDefault());

        context.Items[ItemName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation_id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("CorrelationOrigin", wasSuppliedByClient ? "client" : "generated"))
        using (userId is null ? null : LogContext.PushProperty("UserId", userId))
        {
            await next(context);
        }
    }

    private static string? Normalize(string? candidate) =>
        string.IsNullOrWhiteSpace(candidate) || candidate.Length > MaxAcceptedLength ? null : candidate.Trim();
}
