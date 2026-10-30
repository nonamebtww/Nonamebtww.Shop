using System.Collections.ObjectModel;
using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Profile;
using Nonamebtww.Shop.Mobile.Services.Data;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using Nonamebtww.Shop.Mobile.ViewModels.Base;
using Nonamebtww.Shop.Mobile.ViewModels.Items;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Profile;

/// <summary>«Личные данные» and «Устройства» of the profile: edit name and contacts, end sessions on other devices.</summary>
public sealed partial class PersonalDataViewModel(
  IProfileService profile,
  RemoteDataCache cache,
  INavigationService navigation,
  IDialogService dialogs) : ViewModelBase(navigation, dialogs) {
  private bool _loaded;

  public ObservableCollection<DeviceItem> Devices { get; } = [];

  [Reactive]
  public partial string FirstName { get; set; } = string.Empty;

  [Reactive]
  public partial string LastName { get; set; } = string.Empty;

  [Reactive]
  public partial string Email { get; set; } = string.Empty;

  [Reactive]
  public partial string Phone { get; set; } = string.Empty;

  public override Task OnAppearingAsync() => LoadWithSkeletonAsync(async () => {
    var dto = await ExecuteAsync(() => cache.GetAsync(CacheKeys.Profile, () => profile.GetAsync()), showBusy: false);
    if (dto is not null && !_loaded) {
      Apply(dto);
    }

    await LoadDevicesAsync();
    _loaded = true;
  }, () => _loaded || (cache.Contains(CacheKeys.Profile) && cache.Contains(CacheKeys.Devices)));

  [ReactiveCommand]
  private async Task SaveAsync() {
    var request = new UpdateProfileRequest(FirstName.Trim(), LastName.Trim(), Email.Trim(), Phone.Trim());
    var dto = await ExecuteAsync(() => profile.UpdateAsync(request));
    if (dto is not null) {
      cache.Invalidate(CacheKeys.Profile);
      Apply(dto);
      await Dialogs.AlertAsync("Личные данные", "Изменения сохранены.");
    }
  }

  [ReactiveCommand]
  private async Task SignOutDeviceAsync(DeviceItem device) {
    if (!await Dialogs.ConfirmAsync("Устройства", $"Завершить сеанс на «{device.Name}»?")) {
      return;
    }

    if (await ExecuteAsync(() => profile.SignOutDeviceAsync(device.Id))) {
      cache.Invalidate(CacheKeys.Devices);
      await LoadDevicesAsync();
    }
  }

  private async Task LoadDevicesAsync() {
    var devices = await ExecuteAsync(() => cache.GetAsync(CacheKeys.Devices, () => profile.GetDevicesAsync()), showBusy: false);
    Devices.Clear();
    foreach (var device in devices ?? []) {
      Devices.Add(DeviceItem.From(device));
    }
  }

  private void Apply(ProfileDto dto) {
    FirstName = dto.FirstName;
    LastName = dto.LastName ?? string.Empty;
    Email = dto.Email ?? string.Empty;
    Phone = dto.Phone ?? string.Empty;
  }
}
