using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using MediatR;

namespace Bakery.Reviews.Application.Commands;

public sealed record ChangeCustomerNameCommand(int CustomerId, string NewCustomerName) : ICommand<int>;

public sealed class ChangeCustomerNameCommandHandler(IReviewRepository reviews, ICacheService cache)
    : IRequestHandler<ChangeCustomerNameCommand, int>
{
    public async Task<int> Handle(ChangeCustomerNameCommand request, CancellationToken ct)
    {
        var updated = await reviews.UpdateCustomerNameAsync(request.CustomerId, new PersonName(request.NewCustomerName), ct);
        var ids = await reviews.GetIdsByCustomerAsync(request.CustomerId, ct);

        const string reason = "customer-renamed";
        foreach (var id in ids)
        {
            await cache.RemoveAsync(CacheKeys.Review(id), reason, ct);
        }

        return (int)updated;
    }
}
