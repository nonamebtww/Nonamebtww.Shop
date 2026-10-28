using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.PurchaseAggregate;

public class Purchase : IEntity<Guid> {
  public Guid Id { get; init; }

  // TODO
}