using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.ProductAggregate;

public class Product : IEntity<Guid> {
  public Guid Id { get; init; }
  
  // TODO
}