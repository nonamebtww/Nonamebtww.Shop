using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

public interface ICartsRepository : IRepository<Guid, Cart> {
  public Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
