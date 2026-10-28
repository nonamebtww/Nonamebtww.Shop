using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

public interface IReadOnlyRepository<TKey, TEntity>
  where TKey : struct
  where TEntity : class, IEntity<TKey> {
  // TODO
}