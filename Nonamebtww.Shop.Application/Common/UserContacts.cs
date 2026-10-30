using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Notifications;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

namespace Nonamebtww.Shop.Application.Common;

/// <summary>
/// Куда писать пользователю: на email, а если почты нет — SMS на телефон
/// (регистрация возможна и по почте, и по номеру).
/// </summary>
internal static class UserContacts {
  /// <summary>Письмо или SMS пользователю; null, если контактов нет (удалённый аккаунт).</summary>
  public static NotificationMessage? ToContact(this User user, string subject, string body) =>
    user.Email is { } email ? new NotificationMessage(NotificationChannel.Email, email.Value, subject, body, user.Id)
    : user.Phone is { } phone ? new NotificationMessage(NotificationChannel.Sms, phone.Value, subject, body, user.Id)
    : null;

  public static Task SendToContactAsync(this INotificationService notifications, User user, string subject, string body,
    CancellationToken ct) =>
    user.ToContact(subject, body) is { } message ? notifications.SendAsync(message, ct) : Task.CompletedTask;

  /// <summary>Контакт для подсказки «код отправлен на …»: p***l@yandex.ru или +7 *** ***-**-67.</summary>
  public static string MaskedContact(this User user) =>
    user.Email is { } email ? ConfirmationCodes.MaskEmail(email.Value)
    : user.Phone is { } phone ? ConfirmationCodes.MaskPhone(phone.Value)
    : string.Empty;
}
