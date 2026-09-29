using AutoMapper;
using Bakery.BLL.DTOs;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Bakery.Domain.Exceptions;

namespace Bakery.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public PaymentService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PaymentDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId, ct);
        return payment == null ? null : _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto, CancellationToken ct = default)
    {
        // 1. Валідація існування замовлення
        var order = await _uow.Orders.GetByIdAsync(dto.OrderId, ct);
        if (order == null)
        {
            throw new NotFoundException(nameof(Order), dto.OrderId);
        }

        // 2. Бізнес-перевірки статусів
        if (order.Status == "Paid")
        {
            throw new BusinessConflictException($"Замовлення #{order.OrderNumber} вже успішно оплачено.");
        }

        if (order.Status == "Cancelled")
        {
            throw new BusinessConflictException($"Неможливо провести оплату для скасованого замовлення #{order.OrderNumber}.");
        }

        if (dto.Amount != order.TotalAmount)
        {
            throw new ValidationException(nameof(dto.Amount), 
                $"Сума оплати ({dto.Amount:C}) не відповідає вартості замовлення ({order.TotalAmount:C}).");
        }

        var transactionNumber = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}-UA";

        try
        {
            // 3. Виклик збережуваної процедури в SQL Server
            await _uow.Payments.ProcessPaymentViaProcedureAsync(
                dto.OrderId,
                transactionNumber,
                dto.PaymentMethod,
                dto.Amount,
                "PaymentService",
                ct
            );

            var createdPayment = new Payment
            {
                OrderId = dto.OrderId,
                TransactionNumber = transactionNumber,
                PaymentMethod = dto.PaymentMethod,
                Amount = dto.Amount,
                PaymentDate = DateTime.UtcNow,
                Status = "Completed"
            };

            return _mapper.Map<PaymentDto>(createdPayment);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number >= 50000)
        {
            throw new BusinessConflictException(ex.Message, ex);
        }
    }
}
