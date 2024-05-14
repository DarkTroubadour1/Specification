using Microsoft.EntityFrameworkCore;
using Specification.Interfaces;
using Specification.Models;
using System.Linq.Expressions;

namespace Specification.Data
{
    public static class SpecificationEvaluator<T> where T : BaseEntity
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
        {
            var query = inputQuery;

            if (spec.Criteria != null)
            {
                query = query.Where(spec.Criteria);
            }

            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

            foreach (var includeString in spec.IncludeStrings)
            {
                query = query.Include(includeString);
            }

            if (spec.OrderBy != null)
            {
                query = AddOrderBy(query, spec.OrderBy, spec.OrderAscending);
            }

            if (spec.IsUntracked)
            {
                query = query.AsNoTracking();
            }

            if (!spec.IsPagingEnabled) return query;

            if (spec.OrderBy == null)
            {
                query = AddOrderBy(query, ArgIterator => ArgIterator.Id, true);
            }

            query = query.Skip(spec.Skip).Take(spec.Take);

            return query;
        }

        private static IQueryable<T> AddOrderBy(IQueryable<T> source, Expression<Func<T, object>> keySelector, bool ascending)
        {
            var selectorBody = keySelector.Body;

            if (selectorBody.NodeType == ExpressionType.Convert)
            {
                selectorBody = ((UnaryExpression)selectorBody).Operand;
            }

            var selector = Expression.Lambda(selectorBody, keySelector.Parameters);

            var queryBody = Expression.Call(typeof(Queryable),
                ascending ? "OrderBy" : "OrderByDescending",
                new[] { typeof(T), selectorBody.Type },
                source.Expression, Expression.Quote(selector));

            return source.Provider.CreateQuery<T>(queryBody);
        }
    }
}
