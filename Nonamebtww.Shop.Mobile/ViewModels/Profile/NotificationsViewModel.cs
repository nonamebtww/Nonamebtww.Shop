using System.Collections.ObjectModel;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.Services.Notifications;
using Nonamebtww.Shop.Mobile.ViewModels.Base;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

/// <summary>Messages of the local notification stub (emails, SMS, in-app) addressed to the signed-in user.</summary>
public sealed class NotificationsViewModel(
  ICurrentUserService currentUser,
  NotificationPresenter notifications,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<NotificationItem> Items { get; } = [];

  public override Task OnAppearingAsync() {
    Items.Clear();
    if (currentUser.User is { } user) {
      foreach (var item in notifications.Inbox.Where(x => x.IsFor(user.Id, user.Email, user.Phone))) {
        Items.Add(item);
      }
    }

    return Task.CompletedTask;
  }
}
