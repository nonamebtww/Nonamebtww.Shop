using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Auth;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Mobile.Presentation;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.Services.Notifications;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

/// <summary>Use case «Управление профилем»: personal data, password, devices, sign out.</summary>
public sealed partial class ProfileViewModel(
  IProfileService profile,
  IAuthService auth,
  NotificationPresenter notifications,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  public ObservableCollection<DeviceItem> Devices { get; } = [];

  /// <summary>Messages of the local notification stub (emails, SMS, in-app) addressed to this user.</summary>
  public ObservableCollection<NotificationItem> Notifications { get; } = [];

  [ObservableProperty]
  public partial string DisplayName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string LoginText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string RoleText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string RegisteredText { get; set; } = string.Empty;

  [ObservableProperty]
  public partial bool IsStaff { get; set; }

  [ObservableProperty]
  public partial string FirstName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string LastName { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Email { get; set; } = string.Empty;

  [ObservableProperty]
  public partial string Phone { get; set; } = string.Empty;

  public override async Task OnAppearingAsync() {
    var dto = await ExecuteAsync(() => profile.GetAsync());
    if (dto is not null) {
      Apply(dto);
    }

    Notifications.Clear();
    if (dto is not null) {
      foreach (var item in notifications.Inbox.Where(x => x.IsFor(dto.Id, dto.Email, dto.Phone))) {
        Notifications.Add(item);
      }
    }

    var devices = await ExecuteAsync(() => profile.GetDevicesAsync(), showBusy: false);
    Devices.Clear();
    foreach (var device in devices ?? []) {
      Devices.Add(DeviceItem.From(device));
    }
  }

  [RelayCommand]
  private async Task SaveAsync() {
    var request = new UpdateProfileRequest(FirstName.Trim(), LastName.Trim(), Email.Trim(), Phone.Trim());
    var dto = await ExecuteAsync(() => profile.UpdateAsync(request));
    if (dto is not null) {
      Apply(dto);
      await Dialogs.AlertAsync("Профиль", "Изменения сохранены.");
    }
  }

  [RelayCommand]
  private Task ChangePasswordAsync() => Navigation.GoToAsync(Routes.ChangePassword);

  [RelayCommand]
  private Task OpenStaffPanelAsync() => Navigation.GoToAsync(Routes.Admin);

  [RelayCommand]
  private async Task SignOutDeviceAsync(DeviceItem device) {
    if (!await Dialogs.ConfirmAsync("Устройства", $"Завершить сеанс на «{device.Name}»?")) {
      return;
    }

    if (await ExecuteAsync(() => profile.SignOutDeviceAsync(device.Id))) {
      await OnAppearingAsync();
    }
  }

  [RelayCommand]
  private async Task LogoutAsync() {
    if (!await Dialogs.ConfirmAsync("Выход", "Выйти из аккаунта?")) {
      return;
    }

    if (await ExecuteAsync(() => auth.LogoutAsync())) {
      await Navigation.GoToRootAsync(Routes.Login);
    }
  }

  private void Apply(ProfileDto dto) {
    DisplayName = string.IsNullOrWhiteSpace($"{dto.FirstName}{dto.LastName}") ? dto.Login : $"{dto.FirstName} {dto.LastName}".Trim();
    LoginText = $"@{dto.Login}" + (dto.IsConfirmed ? string.Empty : " · аккаунт не подтверждён");
    RoleText = dto.Role.ToDisplayName();
    RegisteredText = $"С нами с {DisplayText.Date(dto.RegisteredAt)}";
    IsStaff = dto.Role.IsStaff();
    FirstName = dto.FirstName;
    LastName = dto.LastName ?? string.Empty;
    Email = dto.Email;
    Phone = dto.Phone ?? string.Empty;
  }
}
