using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.FavoritesAggregate;

public interface IFavoritesRepository : IRepository<Guid, Favorites> {
  public Task<Favorites?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
