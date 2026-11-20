using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Platform.Aggregator.GrpcClients;

public sealed class GrpcClientLoggingInterceptor(ILogger<GrpcClientLoggingInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var start = DateTimeOffset.UtcNow;
        var methodName = context.Method.FullName;
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            LogResponseAsync(call.ResponseAsync, methodName, start),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> LogResponseAsync<TResponse>(Task<TResponse> responseTask, string methodName, DateTimeOffset start)
    {
        try
        {
            var response = await responseTask;
            logger.LogInformation(
                "gRPC client ok method={Method} elapsed={Elapsed}ms",
                methodName, (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(
                "gRPC client failure method={Method} status={Status} elapsed={Elapsed}ms",
                methodName, ex.Status.StatusCode, (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds);
            throw;
        }
    }
}
