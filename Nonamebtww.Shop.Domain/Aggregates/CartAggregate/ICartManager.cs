using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

public interface ICartManager : IReadOnlyRepository<Guid, Cart> {
  // TODO
}