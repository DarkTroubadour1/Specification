using Specification.Caching;
using Specification.Interfaces;
using Specification.Models;
using System.Linq.Expressions;

namespace Specification.Specifications
{
    public abstract class BaseSpecification<TBaseEntity, TSpecification> : ISpecification<TBaseEntity>
        where TBaseEntity : BaseEntity
        where TSpecification : BaseSpecification<TBaseEntity, TSpecification>
    {
        public Expression<Func<TBaseEntity, bool>> Criteria { get; protected set; }

        public List<Expression<Func<TBaseEntity, object>>> Includes { get; } = new List<Expression<Func<TBaseEntity, object>>>();

        public List<string> IncludeStrings { get; set; } = new List<string>();

        public TimeSpan CacheDuration { get; private set; }

        public bool ShouldCache { get; private set; }

        public Expression<Func<TBaseEntity, object>> OrderBy { get; private set; }

        public bool OrderAscending { get; private set; } = true;

        public int Take { get; private set; }

        public int Skip { get; private set; }

        public bool IsPagingEnabled { get; private set; }

        public bool IsUntracked { get; private set; }

        public void AddInclude(Expression<Func<TBaseEntity, object>> includeExpression) => Includes.Add(includeExpression);

        public string GetCacheKey()
        {
            string body = Criteria.GetCacheKey();
            string order = OrderBy?.Body.ToString();
            string includes = string.Join("-", Includes.Select(i => i.Body));

            var keyNames = new[] { typeof(TBaseEntity).FullName, body, order, OrderAscending.ToString(), includes, $"Take{Take}", $"Skip{Skip}" };
            return string.Join("-", keyNames);
        }

        public TSpecification Cached(int? cacheDurationInSeconds = null)
        {
            EnableCache(cacheDurationInSeconds);
            return (TSpecification)this;
        }

        protected void EnableCache(int? cacheDurationInSeconds = null)
        {
            ShouldCache = true;
            CacheDuration = TimeSpan.FromSeconds(cacheDurationInSeconds.HasValue && cacheDurationInSeconds > 0
                ? cacheDurationInSeconds.Value
                : Constants.CACHING_DEFAULT_DURATION_IN_SECONDS);
        }

        public TSpecification Untracked()
        {
            SetUntracked();
            return (TSpecification)this;
        }

        public void SetUntracked()
        {
            IsUntracked = true;
        }

        protected void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }

        protected void ApplyOrderBy(Expression<Func<TBaseEntity, object>> orderByExpression, bool orderAscending = true)
        {
            OrderBy = orderByExpression;
            OrderAscending = orderAscending;
        }

        public override bool Equals(object? obj)
        {
            if(!(obj is BaseSpecification<TBaseEntity, TSpecification>)) return false;

            string spec1Key = GetCacheKey();
            string spec2Key = ((BaseSpecification<TBaseEntity, TSpecification>)obj).GetCacheKey();

            return spec1Key != null && spec2Key != null && spec1Key == spec2Key;
        }

        public override int GetHashCode() => GetCacheKey().GetHashCode();
    }
}
