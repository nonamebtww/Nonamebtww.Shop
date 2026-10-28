using Nonamebtww.Shop.Domain.Services.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Aggregates.PurchaseAggregate;

public interface IPurchaseManager : IReadOnlyRepository<Guid, Purchase> {
  // TODO
}