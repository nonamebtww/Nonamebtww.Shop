using Nonamebtww.Shop.Application.Models.DTOs.Notifications;

namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;

/// <summary>Внешняя сущность DFD «Сервис уведомлений». В прототипе — локальная заглушка.</summary>
public interface INotificationService {
  Task SendAsync(NotificationMessage message, CancellationToken ct = default);
}
