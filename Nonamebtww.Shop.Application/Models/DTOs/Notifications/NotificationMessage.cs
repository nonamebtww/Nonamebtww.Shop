using Nonamebtww.Shop.Application.Models.Enums;

namespace Nonamebtww.Shop.Application.Models.DTOs.Notifications;

/// <summary>Сообщение «Сервиса уведомлений». Recipient — email, телефон или пусто для InApp.</summary>
public sealed record NotificationMessage(
  NotificationChannel Channel,
  string? Recipient,
  string Subject,
  string Body,
  Guid? UserId = null);
