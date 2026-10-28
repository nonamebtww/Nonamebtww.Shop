using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

public class User : IEntity<Guid> {
  public Guid Id { get; init; }
  
  // TODO
}