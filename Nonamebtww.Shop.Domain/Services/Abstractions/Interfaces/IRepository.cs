using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

/// <summary>
/// Репозиторий агрегата. Изменения фиксируются через <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public interface IRepository<TKey, TEntity> : IReadOnlyRepository<TKey, TEntity>
  where TKey : struct
  where TEntity : class, IEntity<TKey>, IAggregateRoot {
  public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

  public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

  public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default);
}
