using Nonamebtww.Shop.Domain.Entities.Abstractions.Interfaces;
using Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Domain.Entities.Abstractions;

/// <summary>
/// Базовый корень агрегата. События копятся в <see cref="DomainEvents"/>;
/// инфраструктура публикует их после сохранения и вызывает <see cref="ClearDomainEvents"/>.
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot where TKey : struct {
  private readonly List<IDomainEvent> _domainEvents = [];

  protected AggregateRoot() { }

  protected AggregateRoot(TKey id) : base(id) { }

  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

  public void ClearDomainEvents() => _domainEvents.Clear();

  protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
