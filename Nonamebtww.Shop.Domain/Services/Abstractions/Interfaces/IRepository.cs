using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

public interface IRepository<TKey, TEntity> : IReadOnlyRepository<TKey, TEntity>
  where TKey : struct
  where TEntity : class, IEntity<TKey> {
  // TODO
}