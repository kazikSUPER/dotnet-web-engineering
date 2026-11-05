using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using MediatR;

namespace Bakery.Reviews.Application.Commands;

public sealed record ChangeCustomerNameCommand(int CustomerId, string NewCustomerName) : ICommand<int>;

public sealed class ChangeCustomerNameCommandHandler(IReviewRepository reviews) : IRequestHandler<ChangeCustomerNameCommand, int>
{
    public async Task<int> Handle(ChangeCustomerNameCommand request, CancellationToken ct)
    {
        var updated = await reviews.UpdateCustomerNameAsync(request.CustomerId, new PersonName(request.NewCustomerName), ct);

        return (int)updated;
    }
}
