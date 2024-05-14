using Microsoft.EntityFrameworkCore;
using Specification.Extensions;
using Specification.Interfaces;
using Specification.Models;

namespace Specification.Data.Repositories
{
    public class BaseWriteRepository<TDbContext> : IWriteRepository where TDbContext : DbContext
    {
        private readonly TDbContext _context;

        protected BaseWriteRepository(TDbContext context) => _context = context;

        public async Task<T> GetSingleOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity => await spec.GetQuery(_context).SingleOrDefaultAsync(spec.Criteria, cancellationToken);

        public async Task<T> GetSingleAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity => await spec.GetQuery(_context).SingleAsync(spec.Criteria, cancellationToken);

        public async Task<List<T>> ListAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity => await spec.GetQuery(_context).ToListAsync(cancellationToken);

        public async Task<List<T>> ListAllAsync<T>(CancellationToken cancellationToken = default)
            where T : BaseEntity
        {
            return await _context.Set<T>().ToListAsync(cancellationToken);
        }

        public async Task<int> CountAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default)
            where T : BaseEntity => await spec.GetQuery(_context).CountAsync(spec.Criteria, cancellationToken);

        public async Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            return entity;
        }

        public async Task<IList<T>> CreateAsync<T>(IList<T> entityList, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            _context.Set<T>().AddRange(entityList);
            await _context.SaveChangesAsync(cancellationToken);

            return entityList;
        }

        public async Task DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            var fromDb = await _context.Set<T>().FindAsync(id, cancellationToken);
            if (fromDb == null) return;

            _context.Set<T>().Remove(fromDb);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync<T>(IList<T> entityList, CancellationToken cancellationToken = default) where T : BaseEntity
        {
            _context.Set<T>().RemoveRange(entityList);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
