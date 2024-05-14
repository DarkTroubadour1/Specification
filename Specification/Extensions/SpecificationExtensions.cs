using Microsoft.EntityFrameworkCore;
using Specification.Data;
using Specification.Interfaces;
using Specification.Models;

namespace Specification.Extensions
{
    public static class SpecificationExtensions
    {
        public static IQueryable<T> GetQuery<T>(this ISpecification<T> spec, DbContext context) where T : BaseEntity
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (context == null) throw new ArgumentNullException("context");

            return SpecificationEvaluator<T>.GetQuery(context.Set<T>().AsQueryable(), spec);
        }
    }
}
