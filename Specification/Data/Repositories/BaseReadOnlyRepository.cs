using Microsoft.EntityFrameworkCore;
using Specification.Extensions;
using Specification.Interfaces;
using Specification.Models;

namespace Specification.Data.Repositories
{
    public abstract class BaseReadOnlyRepository<TDbContext> : IReadRepository where TDbContext : DbContext
    {
        private readonly TDbContext _context;

        protected BaseReadOnlyRepository(TDbContext context) => _context = context;

        public async Task<T> GetSingleOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            spec.SetUntracked();
            return await spec.GetQuery(_context).SingleOrDefaultAsync(spec.Criteria, cancellationToken);
        }

        public async Task<T> GetSingleAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            spec.SetUntracked();
            return await spec.GetQuery(_context).SingleAsync(spec.Criteria, cancellationToken);
        }

        public async Task<List<T>> ListAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            spec.SetUntracked();

            var query = spec.GetQuery(_context);

            var result = await query.ToListAsync(cancellationToken);

            return result;
        }

        public async Task<List<T>> ListAllAsync<T>(CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity => await spec.GetQuery(_context).CountAsync(spec.Criteria, cancellationToken);
    }
}
