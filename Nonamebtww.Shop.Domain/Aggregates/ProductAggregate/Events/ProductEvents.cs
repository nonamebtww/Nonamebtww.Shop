using Nonamebtww.Shop.Domain.Events.Abstractions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.ProductAggregate.Events;

public sealed record ProductCreated(Guid ProductId, Guid CategoryId, string Sku, string Name) : DomainEvent;

public sealed record ProductPriceChanged(Guid ProductId, Money OldPrice, Money NewPrice) : DomainEvent;

public sealed record ProductStockChanged(Guid ProductId, int OldStock, int NewStock) : DomainEvent;

public sealed record ProductOutOfStock(Guid ProductId) : DomainEvent;

public sealed record ProductActivated(Guid ProductId) : DomainEvent;

public sealed record ProductDeactivated(Guid ProductId) : DomainEvent;
