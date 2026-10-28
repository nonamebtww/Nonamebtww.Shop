using Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;

/// <summary>Корень агрегата: единственная точка изменения агрегата, копит доменные события.</summary>
public interface IAggregateRoot {
  public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

  public void ClearDomainEvents();
}
