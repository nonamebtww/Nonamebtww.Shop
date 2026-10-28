using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

public interface IReadOnlyRepository<TKey, TEntity>
  where TKey : struct
  where TEntity : class, IEntity<TKey> {
  public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

  public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

  public Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}
