using Specification.Models;

namespace Specification.Interfaces
{
    public interface IReadRepository
    {
        Task<T> GetSingleOrDefaultAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<T> GetSingleAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<List<T>> ListAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity;

        Task<List<T>> ListAllAsync<T>(CancellationToken cancellationToken = default)
            where T : BaseEntity;
        Task<int> CountAsync<T>(ISpecification<T> spec, CancellationToken cancellationToken = default) where T : BaseEntity;
    }
}
