using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using System.Linq.Expressions;

namespace Bakery.BLL.Specifications;

public sealed class CustomerOrdersSpecification : ISpecification<Order>
{
    public CustomerOrdersSpecification(int customerId, string? status, int page, int pageSize, string? sortBy, bool descending)
    {
        Criteria = x => x.CustomerId == customerId && !x.IsDeleted && (status == null || x.Status == status);
        Includes.Add(x => x.Items);
        Includes.Add(x => x.Customer!);
        Skip = (page - 1) * pageSize;
        Take = pageSize;

        switch (sortBy?.ToLowerInvariant())
        {
            case "totalamount" when !descending:
                OrderBy = x => x.TotalAmount;
                break;
            case "totalamount":
                OrderByDescending = x => x.TotalAmount;
                break;
            case "status" when !descending:
                OrderBy = x => x.Status;
                break;
            case "status":
                OrderByDescending = x => x.Status;
                break;
            default:
                if (descending) OrderByDescending = x => x.OrderDate;
                else OrderBy = x => x.OrderDate;
                break;
        }
    }

    public Expression<Func<Order, bool>>? Criteria { get; }
    public List<Expression<Func<Order, object>>> Includes { get; } = [];
    public Expression<Func<Order, object>>? OrderBy { get; private set; }
    public Expression<Func<Order, object>>? OrderByDescending { get; private set; }
    public int? Skip { get; }
    public int? Take { get; }
}
