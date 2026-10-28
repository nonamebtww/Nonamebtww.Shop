using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;

namespace Nonamebtww.Shop.Infrastructure.Notifications;

/// <summary>
/// Заглушка «Сервиса уведомлений» (DFD). Ничего не отправляет наружу:
/// пишет в лог, хранит последние сообщения в памяти и поднимает событие,
/// на которое Mobile может подписаться и показать всплывающее уведомление.
/// Коды подтверждения (регистрация, восстановление пароля) тоже приходят сюда.
/// </summary>
public sealed class LocalNotificationService : INotificationService {
  private const int MaxStored = 100;

  private readonly ILogger<LocalNotificationService> _logger;
  private readonly ConcurrentQueue<NotificationMessage> _sent = new();

  public LocalNotificationService(ILogger<LocalNotificationService> logger) {
    _logger = logger;
  }

  /// <summary>Срабатывает на каждое «отправленное» уведомление.</summary>
  public event EventHandler<NotificationMessage>? NotificationSent;

  /// <summary>Последние уведомления, от старых к новым.</summary>
  public IReadOnlyList<NotificationMessage> Sent => _sent.ToArray();

  public Task SendAsync(NotificationMessage message, CancellationToken ct = default) {
    ArgumentNullException.ThrowIfNull(message);

    _sent.Enqueue(message);

    while (_sent.Count > MaxStored) {
      _sent.TryDequeue(out _);
    }

    _logger.LogInformation(
      "[Уведомление:{Channel}] кому: {Recipient}; тема: {Subject}; текст: {Body}",
      message.Channel, message.Recipient ?? "-", message.Subject, message.Body);

    NotificationSent?.Invoke(this, message);
    return Task.CompletedTask;
  }
}
