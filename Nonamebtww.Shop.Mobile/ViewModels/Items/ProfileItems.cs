using Nonamebtww.Shop.Application.Models.DTOs.Profile;
using Nonamebtww.Shop.Mobile.Presentation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Items;

public sealed record DeviceItem(Guid Id, string Name, string Details, bool CanSignOut) {
  public static DeviceItem From(DeviceDto device) => new(
    device.Id,
    device.IsCurrent ? $"{device.Name} (это устройство)" : device.Name,
    $"{(device.IsActive ? "Активно" : "Не активно")} · вход {DisplayText.Date(device.LastLoginAt)}",
    device.IsActive && !device.IsCurrent);
}
