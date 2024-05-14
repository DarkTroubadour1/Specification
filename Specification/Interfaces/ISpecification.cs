using Specification.Models;
using System.Linq.Expressions;

namespace Specification.Interfaces
{
    public interface ISpecification<TBaseEntity> where TBaseEntity : BaseEntity
    {
        Expression<Func<TBaseEntity, bool>> Criteria { get; }
        List<Expression<Func<TBaseEntity, object>>> Includes { get; }
        void AddInclude(Expression<Func<TBaseEntity, object>> includeExpression);
        List<string> IncludeStrings { get; set; }
        TimeSpan CacheDuration { get; }
        bool ShouldCache { get; }
        string GetCacheKey();
        Expression<Func<TBaseEntity, object>> OrderBy { get; }
        bool OrderAscending { get; }
        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
        bool IsUntracked { get; }
        void SetUntracked();
    }
}
