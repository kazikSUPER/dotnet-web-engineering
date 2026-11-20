using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            var correlationId = Resolve(request);

            if (correlationId is not null)
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private string? Resolve(HttpRequestMessage request)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext?.Items[CorrelationIdMiddleware.ItemName] is string correlationId)
        {
            return correlationId;
        }

        var activity = Activity.Current;

        if (activity is not null)
        {
            return activity.TraceId.ToString();
        }

        return request.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var supplied)
            ? supplied.FirstOrDefault()?.ToString()
            : null;
    }
}
