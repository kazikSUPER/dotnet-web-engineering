using AutoMapper;
using Bakery.BLL.DTOs.Payments;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class PaymentService(IUnitOfWork uow, IMapper mapper) : IPaymentService
{
    public async Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default) => mapper.Map<PaymentResponseDto>(await uow.Payments.GetByOrderIdAsync(orderId, ct));

    public async Task<PaymentResponseDto> ProcessAsync(ProcessPaymentRequest request, CancellationToken ct = default)
    {
        var order = await uow.Orders.GetWithExplicitPaymentAsync(request.OrderId, ct) ?? throw new NotFoundException(nameof(Order), request.OrderId);
        if (order.Payment is not null) throw new BusinessConflictException("Оплата для цього замовлення вже проведена.");
        if (order.Status is "Paid" or "Cancelled") throw new BusinessConflictException("Оплату для цього замовлення провести неможливо.");
        if (request.Amount != order.TotalAmount) throw new ValidationException(nameof(request.Amount), "Сума оплати не відповідає вартості замовлення.");
        var payment = new Payment(order.Id, $"TXN-{Guid.NewGuid():N}"[..16], request.PaymentMethod, request.Amount);
        await uow.Payments.AddAsync(payment, ct);
        order.ChangeStatus("Paid");
        uow.Orders.Update(order);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<PaymentResponseDto>(payment);
    }
}
