using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.CategoryAggregate;

public class Category : IEntity<Guid> {
  public Guid Id { get; init; }
  
  // TODO
}