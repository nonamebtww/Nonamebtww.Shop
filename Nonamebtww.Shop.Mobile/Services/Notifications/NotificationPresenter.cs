using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Nonamebtww.Shop.Application.Models.DTOs.Notifications;
using Nonamebtww.Shop.Application.Models.Enums;
using Nonamebtww.Shop.Infrastructure.Notifications;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;

namespace Nonamebtww.Shop.Mobile.Services.Notifications;

public sealed record NotificationItem(string Title, string Body, string Time, Guid? UserId, string? Recipient) {
  /// <summary>The message belongs to the user (by id, or by email / phone for messages sent before sign-in).</summary>
  public bool IsFor(Guid? userId, params string?[] contacts) =>
    (UserId is not null && UserId == userId)
    || (UserId is null && Recipient is not null && contacts.Any(x => string.Equals(x, Recipient, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>
/// Makes the local notification stub visible: every «sent» email / SMS / in-app message goes to an inbox
/// shown in the profile, and messages with a confirmation code (registration, password reset) pop up,
/// so the prototype works without any real mail or SMS service.
/// </summary>
public sealed partial class NotificationPresenter(LocalNotificationService notifications, IDialogService dialogs) {
  private const int MaxItems = 30;

  private readonly SemaphoreSlim _alertGate = new(1, 1);
  private bool _started;

  public ObservableCollection<NotificationItem> Inbox { get; } = [];

  public void Start() {
    if (_started) {
      return;
    }

    _started = true;
    notifications.NotificationSent += OnNotificationSent;
  }

  private void OnNotificationSent(object? sender, NotificationMessage message) {
    var channel = message.Channel switch {
      NotificationChannel.Email => "Письмо",
      NotificationChannel.Sms => "SMS",
      _ => "Уведомление"
    };

    var to = string.IsNullOrWhiteSpace(message.Recipient) ? string.Empty : $" для {message.Recipient}";
    var title = $"{channel}{to}";
    var item = new NotificationItem($"{message.Subject} · {channel}", message.Body, DisplayText.Date(DateTimeOffset.Now), message.UserId, message.Recipient);

    MainThread.BeginInvokeOnMainThread(() => {
      Inbox.Insert(0, item);
      while (Inbox.Count > MaxItems) {
        Inbox.RemoveAt(Inbox.Count - 1);
      }
    });

    if (CodeRegex().IsMatch(message.Body)) {
      _ = ShowAsync(title, $"{message.Subject}\n\n{message.Body}");
    }
  }

  private async Task ShowAsync(string title, string text) {
    await _alertGate.WaitAsync();
    try {
      await dialogs.AlertAsync(title, text);
    } finally {
      _alertGate.Release();
    }
  }

  [GeneratedRegex(@"\b\d{6}\b")]
  private static partial Regex CodeRegex();
}
