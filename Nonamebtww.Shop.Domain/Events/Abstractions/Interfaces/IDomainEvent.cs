namespace Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

public interface IDomainEvent {
  public Guid EventId { get; }
  public DateTimeOffset OccurredAt { get; }
}
