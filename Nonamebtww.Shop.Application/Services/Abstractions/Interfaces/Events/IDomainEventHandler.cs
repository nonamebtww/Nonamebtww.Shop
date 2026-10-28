using Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Events;

/// <summary>
/// Обработчик доменного события. Infrastructure вызывает обработчики после коммита IUnitOfWork.
/// Основные побочные эффекты (уведомления, остатки, корзина) use case выполняют явно,
/// поэтому обработчики — точка расширения (аналитика, синхронизация с будущим API).
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent {
  Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
