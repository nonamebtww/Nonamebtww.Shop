using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

public class CartItem : IEntity<Guid> {
  public Guid Id { get; init; }
  
  // TODO
}