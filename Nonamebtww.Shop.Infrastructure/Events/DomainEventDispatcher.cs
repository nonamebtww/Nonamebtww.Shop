using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Events;
using Nonamebtww.Shop.Domain.Events.Abstractions.Interfaces;

namespace Nonamebtww.Shop.Infrastructure.Events;

/// <summary>
/// Публикует доменные события зарегистрированным IDomainEventHandler&lt;TEvent&gt; после коммита.
/// Ошибка обработчика не откатывает уже сохранённые данные: она пишется в лог.
/// </summary>
internal sealed class DomainEventDispatcher {
  private static readonly ConcurrentDictionary<Type, MethodInfo> DispatchMethods = new();

  private static readonly MethodInfo DispatchGeneric =
    typeof(DomainEventDispatcher).GetMethod(nameof(DispatchOneAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

  private readonly IServiceProvider _services;
  private readonly ILogger<DomainEventDispatcher> _logger;

  public DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger) {
    _services = services;
    _logger = logger;
  }

  public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct) {
    foreach (var domainEvent in events) {
      var method = DispatchMethods.GetOrAdd(domainEvent.GetType(), t => DispatchGeneric.MakeGenericMethod(t));
      await (Task)method.Invoke(this, [domainEvent, ct])!;
    }
  }

  private async Task DispatchOneAsync<TEvent>(TEvent domainEvent, CancellationToken ct) where TEvent : IDomainEvent {
    foreach (var handler in _services.GetServices<IDomainEventHandler<TEvent>>()) {
      try {
        await handler.HandleAsync(domainEvent, ct);
      } catch (Exception ex) when (ex is not OperationCanceledException) {
        _logger.LogError(ex, "Обработчик {Handler} упал на событии {Event}", handler.GetType().Name, typeof(TEvent).Name);
      }
    }
  }
}
