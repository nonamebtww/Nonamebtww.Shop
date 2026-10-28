using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CartAggregate;

public class Cart : IEntity<Guid> {
  public Guid Id { get; init; }
  
  // TODO
}