using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Orders.Api.Grpc;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var start = DateTimeOffset.UtcNow;
        var method = context.Method;
        var peer = context.Peer;

        try
        {
            var response = await continuation(request, context);
            logger.LogInformation(
                "gRPC ok method={Method} peer={Peer} status={Status} elapsed={Elapsed}ms",
                method, peer, context.Status.StatusCode, (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(
                ex,
                "gRPC error method={Method} peer={Peer} status={Status} elapsed={Elapsed}ms",
                method, peer, ex.StatusCode, (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "gRPC error method={Method} peer={Peer} elapsed={Elapsed}ms",
                method, peer, (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds);
            throw;
        }
    }
}
