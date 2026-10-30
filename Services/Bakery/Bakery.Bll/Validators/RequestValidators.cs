using Bakery.BLL.DTOs.Customers;
using Bakery.BLL.DTOs.Orders;
using Bakery.BLL.DTOs.Payments;
using FluentValidation;

namespace Bakery.BLL.Validators;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator() { RuleFor(x => x.Email).EmailAddress(); RuleFor(x => x.PhoneNumber).NotEmpty(); }
}

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator() { RuleFor(x => x.Items).NotEmpty(); RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator()); }
}

public sealed class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator() { RuleFor(x => x.ProductId).GreaterThan(0); RuleFor(x => x.Quantity).InclusiveBetween(1, 100); }
}

public sealed class ProcessPaymentRequestValidator : AbstractValidator<ProcessPaymentRequest>
{
    public ProcessPaymentRequestValidator() { RuleFor(x => x.OrderId).GreaterThan(0); RuleFor(x => x.Amount).GreaterThan(0); }
}
