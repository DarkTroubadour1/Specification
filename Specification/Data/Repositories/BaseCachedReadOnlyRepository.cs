using Microsoft.EntityFrameworkCore;
using Specification.Interfaces;
using Specification.Models;

namespace Specification.Data.Repositories
{
    public abstract class BaseCachedReadOnlyRepository<TDbContext> : IReadRepository where TDbContext : DbContext
    {
        private readonly BaseReadOnlyRepository<TDbContext> _internalRepo;
        private readonly ICacheProvider _cacheProvider;

        protected BaseCachedReadOnlyRepository(BaseReadOnlyRepository<TDbContext> internalRepo, ICacheProvider cacheProvider)
        {
            _internalRepo = internalRepo;
            _cacheProvider = cacheProvider;
        }

        public Task<int> CountAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return _internalRepo.CountAsync(spec, cancellationToken);
        }

        public Task<T> GetSingleAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return _internalRepo.GetSingleAsync(spec, cancellationToken);
        }

        public Task<T> GetSingleOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return _internalRepo.GetSingleOrDefaultAsync(spec, cancellationToken);
        }

        public Task<List<T>> ListAllAsync<T>(CancellationToken cancellationToken = default) where T : BaseEntity
        {
            return _internalRepo.ListAllAsync<T>(cancellationToken);
        }

        public Task<List<T>> ListAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            if (spec.ShouldCache)
            {
                return _cacheProvider.GetAndSetAsync(spec.GetCacheKey(), () => _internalRepo.ListAsync<T>(spec, cancellationToken), spec.CacheDuration);
            }
            else
            {
                return _internalRepo.ListAsync<T>(spec, cancellationToken);
            }
        }
    }
}
