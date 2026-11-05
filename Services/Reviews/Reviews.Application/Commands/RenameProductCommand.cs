using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;

namespace Bakery.Reviews.Application.Commands;

public sealed record RenameProductCommand(int ProductId, string NewProductName) : ICommand<int>;

public sealed class RenameProductCommandHandler(IReviewRepository reviews) : IRequestHandler<RenameProductCommand, int>
{
    public async Task<int> Handle(RenameProductCommand request, CancellationToken ct)
    {
        var updated = await reviews.UpdateProductNameAsync(request.ProductId, request.NewProductName, ct);

        return (int)updated;
    }
}
