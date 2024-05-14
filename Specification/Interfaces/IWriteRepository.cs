using Specification.Models;

namespace Specification.Interfaces
{
    public interface IWriteRepository : IReadRepository
    {
        Task<T> CreateAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task<IList<T>> CreateAsync<T>(IList<T> entityList, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task DeleteAsync<T>(int id, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task DeleteAsync<T>(IList<T> entityList, CancellationToken cancellationToken = default) where T : BaseEntity;
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
