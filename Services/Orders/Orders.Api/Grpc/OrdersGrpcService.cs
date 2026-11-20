using System.Globalization;
using Grpc.Core;
using Orders.Api.Grpc;
using Orders.BLL.Interfaces;

namespace Orders.Api.Grpc;

public sealed class OrdersGrpcService(IOrderService orders) : OrdersService.OrdersServiceBase
{
    public override async Task<OrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order id must be positive."));
        }

        var order = await orders.GetByIdAsync(request.Id, context.CancellationToken);
        if (order is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.Id} was not found."));
        }

        var reply = new OrderReply
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName ?? string.Empty,
            Status = order.Status,
            TotalAmount = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture),
            OrderDate = order.OrderDate.ToString("O"),
        };

        foreach (var item in order.Items)
        {
            reply.Items.Add(new OrderItemReply
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                Quantity = item.Quantity,
                TotalPrice = item.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
            });
        }

        return reply;
    }
}
