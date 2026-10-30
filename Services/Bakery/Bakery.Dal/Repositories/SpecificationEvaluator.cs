using Bakery.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public static class SpecificationEvaluator
{
    public static IQueryable<T> GetQuery<T>(IQueryable<T> input, ISpecification<T> specification, bool applyPaging = true) where T : class
    {
        var query = input;
        if (specification.Criteria is not null) query = query.Where(specification.Criteria);
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        if (specification.OrderBy is not null) query = query.OrderBy(specification.OrderBy);
        if (specification.OrderByDescending is not null) query = query.OrderByDescending(specification.OrderByDescending);
        if (applyPaging && specification.Skip.HasValue) query = query.Skip(specification.Skip.Value);
        if (applyPaging && specification.Take.HasValue) query = query.Take(specification.Take.Value);
        return query;
    }
}
