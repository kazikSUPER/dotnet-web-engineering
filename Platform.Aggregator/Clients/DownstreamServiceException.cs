using System.Net;

namespace Platform.Aggregator.Clients;

public sealed class DownstreamServiceException(
    string serviceName,
    string path,
    HttpStatusCode? statusCode,
    string reason) : Exception($"{serviceName} відхилив виклик {path}: {reason}")
{
    public string ServiceName { get; } = serviceName;

    public string Path { get; } = path;

    public HttpStatusCode? StatusCode { get; } = statusCode;

    public string Reason { get; } = reason;
}
