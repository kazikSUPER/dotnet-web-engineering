using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Orders.BLL.Diagnostics;
using Orders.BLL.DTOs.Payments;
using Orders.BLL.Interfaces;
using Orders.DAL.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;
using System.Diagnostics;
using ValidationException = Orders.Domain.Exceptions.ValidationException;

namespace Orders.BLL.Services;

public class PaymentService(
    IUnitOfWork uow,
    IMapper mapper,
    IValidator<ProcessPaymentRequest> processValidator,
    ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) => mapper.Map<PaymentResponseDto>(await uow.Payments.GetByOrderIdAsync(orderId, ct));

    public async Task<PaymentResponseDto> ProcessAsync(ProcessPaymentRequest request, CancellationToken ct = default)
    {
        using var activity = OrdersActivitySources.Payments.StartActivity("payment.process", ActivityKind.Internal);
        activity?.SetTag("payment.order_id", request.OrderId);
        activity?.SetTag("payment.method", request.PaymentMethod);
        activity?.SetTag("payment.amount", request.Amount);

        await processValidator.ValidateAndThrowAsync(request, ct);
        var order = await uow.Orders.GetWithExplicitPaymentAsync(request.OrderId, ct) ?? throw new NotFoundException(nameof(Order), request.OrderId);
        if (order.Payment is not null) throw new BusinessConflictException("Оплата для цього замовлення вже проведена.");
        if (order.Status is "Paid" or "Cancelled") throw new BusinessConflictException("Оплату для цього замовлення провести неможливо.");
        if (request.Amount != order.TotalAmount) throw new ValidationException(nameof(request.Amount), "Сума оплати не відповідає вартості замовлення.");
        var payment = new Payment(order.Id, $"TXN-{Guid.NewGuid():N}"[..16], request.PaymentMethod, request.Amount);
        await uow.Payments.AddAsync(payment, ct);
        order.ChangeStatus("Paid");
        uow.Orders.Update(order);
        await uow.SaveChangesAsync(ct);

        activity?.SetTag("payment.transaction_number", payment.TransactionNumber);
        activity?.SetTag("order.status", order.Status);

        logger.LogInformation(
            "Оплата {TransactionNumber} проведена для замовлення {OrderId}: сума {Amount}, спосіб {PaymentMethod}, статус замовлення {Status}",
            payment.TransactionNumber, order.Id, payment.Amount, payment.PaymentMethod, order.Status);

        return mapper.Map<PaymentResponseDto>(payment);
    }
}
