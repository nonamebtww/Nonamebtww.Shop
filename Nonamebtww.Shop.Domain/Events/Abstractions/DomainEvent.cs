using Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Events.Abstractions;

public abstract record DomainEvent : IDomainEvent {
  public Guid EventId { get; init; } = Guid.NewGuid();
  public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
