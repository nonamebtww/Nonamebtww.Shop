using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;

namespace Nonamebtww.Shop.Mobile.Services.Platform;

/// <summary>Device name for the «Устройство» entity (shown in the profile's device list).</summary>
public sealed class MauiDeviceInfoProvider : IDeviceInfoProvider {
  public string DeviceName => $"{DeviceInfo.Current.Name} ({DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString})";
  public string? IpAddress => null;
}
