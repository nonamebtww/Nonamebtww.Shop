using Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Common;

namespace Nonamebtww.Shop.Infrastructure.Common;

/// <summary>
/// Сведения об устройстве без зависимости от MAUI.
/// Mobile может зарегистрировать свою реализацию поверх Microsoft.Maui.Devices.DeviceInfo.
/// </summary>
public sealed class DefaultDeviceInfoProvider : IDeviceInfoProvider {
  public string DeviceName { get; } = $"{Environment.MachineName} ({Environment.OSVersion.Platform})";

  // В прототипе сети нет, поэтому IP-адрес не определяется.
  public string? IpAddress => null;
}
